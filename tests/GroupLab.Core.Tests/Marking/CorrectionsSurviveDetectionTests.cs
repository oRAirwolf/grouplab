using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 42: a corrected shot does not survive a second detection.
/// <para>
/// <b>What was wrong.</b> Detecting again kept only the shots placed by hand. Every hole a person had dragged onto the right place, or moved
/// to the bull they actually aimed at, went back to where the detector had put it, and nothing on the screen said so. Somebody who settled a
/// sheet and then pressed detect because they had entered the calibre lost the lot.
/// </para>
/// <para>
/// <b>The rule.</b> A corrected shot is matched to the nearest fresh detection within one hole's width. Where there is one, the person's
/// position and chosen bull win and the detection is dropped, so one hole never becomes two marks. Where there is none, the corrected shot
/// survives on its own, because nothing the detector found is the same mark.
/// </para>
/// </summary>
public class CorrectionsSurviveDetectionTests
{
    private const double Inch = 100;

    /// <summary>A hundred pixels to the inch, so a pixel is a hundredth of an inch and the arithmetic below reads as inches.</summary>
    private static ScaleReference Scale() => new LengthReference(new PointD(0, 0), new PointD(Inch, 0), 1);

    private static BullAim[] Bulls() =>
    [
        new(1, "1", new PointD(100, 100)),
        new(2, "2", new PointD(400, 100)),
        new(3, "3", new PointD(700, 100)),
    ];

    /// <summary>One round of detection, as the simple path loads it.</summary>
    private static void Detect(MarkingSession session, params (double X, double Y, int? Bull)[] holes) =>
        session.LoadDetections(Scale(), Bulls(), [.. holes.Select(h => (new PointD(h.X, h.Y), h.Bull))], "a test sheet");

    private static MarkingSession WithOneCorrection(out int moved)
    {
        var session = new MarkingSession();
        Detect(session, (100, 100, 1), (400, 100, 2));

        // The person disagrees with where the detector put the second hole and nudges it a tenth of an inch, which is well inside one hole's
        // width. A drag further than that is a different mark, and the test below pins that.
        var shot = session.State.Shots.Single(s => s.Bull == 2);
        session.MoveShot(shot.Id, new PointD(410, 100));
        moved = shot.Id;

        Assert.Equal(ShotProvenance.Corrected, session.State.Find(moved)!.Provenance);
        return session;
    }

    [Fact]
    public void ACorrectionSurvivesASecondDetection()
    {
        var session = WithOneCorrection(out int moved);

        // The detector runs again and finds the same two holes where it found them the first time.
        Detect(session, (100, 100, 1), (400, 100, 2));

        Assert.Equal(2, session.State.Shots.Count);

        var kept = session.State.Find(moved);
        Assert.NotNull(kept);
        Assert.Equal(410, kept!.Image.X, 6);
        Assert.Equal(ShotProvenance.Corrected, kept.Provenance);
    }

    /// <summary>
    /// The half that would be worse than losing the correction: keeping it and adding the detection beside it, so the sheet gains a shot
    /// nobody fired and the group grows by a hole that is not there.
    /// </summary>
    [Fact]
    public void TheDetectionItReplacesIsNotAddedBesideIt()
    {
        var session = WithOneCorrection(out _);
        Detect(session, (100, 100, 1), (400, 100, 2));

        var near = session.State.Shots.Where(s => Math.Abs(s.Image.X - 410) < 30).ToList();
        Assert.Single(near);
        Assert.Equal(2, session.State.Shots.Count(s => s.IsShot));
    }

    /// <summary>
    /// A correction moved further than one hole's width is a different mark, so it survives on its own and the detection stays too. This is
    /// the case where a person found a hole the detector had missed and dragged a spare mark onto it.
    /// </summary>
    [Fact]
    public void ACorrectionMovedBeyondOneHolesWidthSurvivesOnItsOwn()
    {
        var session = new MarkingSession();
        Detect(session, (100, 100, 1), (400, 100, 2));

        var shot = session.State.Shots.Single(s => s.Bull == 2);
        session.MoveShot(shot.Id, new PointD(700, 100));

        Detect(session, (100, 100, 1), (400, 100, 2));

        Assert.Equal(3, session.State.Shots.Count);
        Assert.Contains(session.State.Shots, s => Math.Abs(s.Image.X - 700) < 1);
        Assert.Contains(session.State.Shots, s => Math.Abs(s.Image.X - 400) < 1);
    }

    /// <summary>A shot placed by hand was never the detector's, and is kept as it always was.</summary>
    [Fact]
    public void AShotPlacedByHandIsStillKept()
    {
        var session = new MarkingSession();
        Detect(session, (100, 100, 1));
        int byHand = session.AddShot(new PointD(700, 100), 3);
        Assert.Equal(ShotProvenance.Manual, session.State.Find(byHand)!.Provenance);

        Detect(session, (100, 100, 1));

        Assert.Equal(2, session.State.Shots.Count);
        Assert.Equal(ShotProvenance.Manual, session.State.Find(byHand)!.Provenance);
    }

    /// <summary>
    /// Entry 143, question 42: "make the button honest". The count the button shows is the number of marks that would be kept, and it is
    /// nought on a sheet nobody has touched, so the button reads as it always did until there is something to say.
    /// </summary>
    [Fact]
    public void TheCountTheButtonShowsIsWhatWouldBeKept()
    {
        var session = new MarkingSession();
        Detect(session, (100, 100, 1), (400, 100, 2));
        Assert.Equal(0, session.CorrectionsThatWouldBeKept());

        var shot = session.State.Shots.Single(s => s.Bull == 2);
        session.MoveShot(shot.Id, new PointD(410, 100));
        Assert.Equal(1, session.CorrectionsThatWouldBeKept());

        session.AddShot(new PointD(700, 100), 3);
        Assert.Equal(2, session.CorrectionsThatWouldBeKept());
    }

    /// <summary>
    /// Undo covers the whole of it, because a detection that keeps corrections is still one step. Without this, a person who detected again
    /// and did not like the result would have no way back to the sheet they had settled.
    /// </summary>
    [Fact]
    public void UndoPutsTheSheetBackAsItWas()
    {
        var session = WithOneCorrection(out int moved);
        int before = session.State.Shots.Count;

        Detect(session, (100, 100, 1), (400, 100, 2), (700, 100, 3));
        Assert.Equal(3, session.State.Shots.Count);

        session.Undo();

        Assert.Equal(before, session.State.Shots.Count);
        Assert.Equal(410, session.State.Find(moved)!.Image.X, 6);
    }
}
