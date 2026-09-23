using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 41: dragging always moves the shot, and never changes which bull it belongs to.
/// <para>
/// <b>Why it is a rule and not a preference.</b> Entry 141 section 5.3 item 3 listed three ways to assign a shot to a bull, and the first was
/// dragging it onto the bull. That makes one gesture mean two things: a mark's position is a measurement, and a drag is how a person corrects
/// it. If dropping a mark near bull 7 also pinned it to bull 7, then somebody nudging a mark a sixteenth of an inch onto the hole would be
/// making a decision they never took, and the matching would stop being allowed to reconsider it.
/// </para>
/// <para>
/// <b>It was never built</b>, so nothing was removed from the application. This is what stops it arriving by accident, because the gesture is
/// an obvious one to reach for and nothing else in the code would object.
/// </para>
/// <para>
/// A drag can still change which bull a shot is <i>matched</i> to, and that is a different thing: a shot nobody has assigned is re-solved
/// after every edit (entry 70 section 3), so moving a mark two bulls to the left changes the answer the matching gives. What the drag must
/// never do is make that answer a person's choice, which is what <see cref="MarkedShot.BullChosen"/> records.
/// </para>
/// </summary>
public class DragNeverAssignsTests
{
    private static BullAim[] Bulls() =>
    [
        new(1, "1", new PointD(100, 100)),
        new(2, "2", new PointD(400, 100)),
        new(3, "3", new PointD(700, 100)),
    ];

    private static MarkingSession Sheet()
    {
        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), Bulls(),
            [(new PointD(100, 100), 1), (new PointD(400, 100), 2)], "a test sheet");
        return session;
    }

    /// <summary>A mark dropped exactly on another bull is a mark in a new place, not a decision about which bull it belongs to.</summary>
    [Fact]
    public void DroppingAMarkOnTopOfABullDoesNotChooseThatBull()
    {
        var session = Sheet();
        var shot = session.State.Shots.First(s => s.Bull == 1);
        Assert.False(shot.BullChosen);

        session.MoveShot(shot.Id, new PointD(700, 100));

        var after = session.State.Find(shot.Id)!;
        Assert.False(after.BullChosen, "a drag pinned the shot to a bull, so one gesture now means two things");
        Assert.Equal(ShotProvenance.Corrected, after.Provenance);
        Assert.Equal(700, after.Image.X, 6);
    }

    /// <summary>
    /// And the other way about: a bull a person did choose survives a drag, so correcting where a mark sits never quietly undoes the decision
    /// they took about which bull it was fired at.
    /// </summary>
    [Fact]
    public void AChosenBullSurvivesADragToTheOtherEndOfTheSheet()
    {
        var session = Sheet();
        var shot = session.State.Shots.First(s => s.Bull == 1);
        session.AssignBull(shot.Id, 3);

        var chosen = session.State.Find(shot.Id)!;
        Assert.True(chosen.BullChosen);
        Assert.Equal(3, chosen.Bull);

        session.MoveShot(shot.Id, new PointD(400, 100));

        var after = session.State.Find(shot.Id)!;
        Assert.True(after.BullChosen, "a drag threw away a bull the person had chosen");
        Assert.Equal(3, after.Bull);
        Assert.Equal(400, after.Image.X, 6);
    }

    /// <summary>
    /// The ways a bull may be chosen are the three entry 143 leaves: the picker and the keyboard both arrive here, and assigning several at
    /// once arrives at the same place. Each marks the choice as the person's; moving does not.
    /// </summary>
    [Fact]
    public void ChoosingABullIsWhatMarksItChosen()
    {
        var session = Sheet();
        var shots = session.State.Shots.OrderBy(s => s.Id).ToList();

        session.AssignBull(shots[0].Id, 2);
        Assert.True(session.State.Find(shots[0].Id)!.BullChosen);

        session.AssignBulls([shots[1].Id], 3);
        Assert.True(session.State.Find(shots[1].Id)!.BullChosen);

        foreach (var shot in session.State.Shots)
        {
            session.MoveShot(shot.Id, new PointD(shot.Image.X + 5, shot.Image.Y));
        }

        Assert.All(session.State.Shots, s => Assert.True(s.BullChosen, "moving a shot must not change whose decision its bull was"));
    }
}
