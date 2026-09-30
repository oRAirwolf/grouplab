using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 318 section 2: Find holes on a target GroupLab did not print. Its proposals become normal marks the person can
/// confirm, move or remove, in one step Undo takes back; one the finder was unsure of goes to the review queue until the person settles it;
/// finding again replaces only what nobody touched; and it is offered only where the scale was set by hand, always labelled Experimental.
/// </summary>
public class FindHolesTests
{
    // A hundred pixels to the inch, two aim points placed by hand, and a .308.
    private static MarkingSession Marked()
    {
        var session = new MarkingSession();
        session.Open("target.png");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetCalibre(Calibre.Of(0.308));
        session.AddBull(new PointD(200, 200));
        session.AddBull(new PointD(600, 200));
        return session;
    }

    private static readonly ProposedHole[] Proposals =
    [
        new(new PointD(210, 190), 0.30, HoleLook.DarkOnPaper, null),
        new(new PointD(590, 215), 0.12, HoleLook.LightInPrint, "it is 0.12 in across, small for the bullet: it may be printing or a speck"),
        new(new PointD(230, 240), 0.29, HoleLook.DarkInBrightRing, null),
    ];

    [Fact]
    public void ProposalsBecomeMarksOnTheirNearestBullsAndUndoTakesThemAllBack()
    {
        var session = Marked();
        int placed = session.ProposeHoles(Proposals);

        Assert.Equal(3, placed);
        var shots = session.State.Shots;
        Assert.All(shots, s => Assert.Equal(ShotProvenance.Automatic, s.Provenance));
        Assert.All(shots, s => Assert.NotNull(s.Proposal));
        Assert.Equal(new[] { 0, 1, 0 }, shots.Select(s => s.Bull ?? -1));
        Assert.Equal(0.30, shots[0].MeasuredDiameterInches);
        Assert.Equal("add 3 shots", session.UndoWords);

        session.Undo();
        Assert.Empty(session.State.Shots);
    }

    [Fact]
    public void OnlyTheDoubtfulOneGoesToTheReviewAndSettlingItClearsIt()
    {
        var session = Marked();
        session.ProposeHoles(Proposals);
        var doubtful = session.State.Shots[1];

        var item = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Proposed);
        Assert.Equal(doubtful.Id, item.ShotId);
        Assert.Contains("experimental", item.Sentence, StringComparison.Ordinal);
        Assert.Contains("small for the bullet", item.Sentence, StringComparison.Ordinal);
        Assert.Single(ReviewQueue.MarksToCheck(session.State));

        ReviewQueue.Apply(session, item, item.Choices.Single(c => c.Label == "It is a hole"));
        Assert.False(ReviewQueue.StillDoubted(session.State, session.State.Find(doubtful.Id)!));
        Assert.Empty(ReviewQueue.MarksToCheck(session.State));
        Assert.True(ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Proposed).Resolved);
    }

    [Fact]
    public void MovingOrRemovingADoubtfulProposalSettlesIt()
    {
        var session = Marked();
        session.ProposeHoles(Proposals);
        int id = session.State.Shots[1].Id;

        session.MoveShot(id, new PointD(595, 212));
        Assert.Equal(ShotProvenance.Corrected, session.State.Find(id)!.Provenance);
        Assert.Empty(ReviewQueue.MarksToCheck(session.State));

        session.Undo();
        session.SetNotAShot(id, true);
        Assert.Empty(ReviewQueue.MarksToCheck(session.State));
    }

    [Fact]
    public void FindingAgainReplacesWhatNobodyTouchedAndKeepsWhatThePersonDid()
    {
        var session = Marked();
        session.ProposeHoles(Proposals);
        int moved = session.State.Shots[0].Id;
        session.MoveShot(moved, new PointD(212, 192));
        int byHand = session.AddShot(new PointD(400, 400));

        // The same holes again: the moved one and the one by hand are kept, and the proposal on the moved hole is not placed beside it.
        int placed = session.ProposeHoles(Proposals);
        Assert.Equal(2, placed);
        Assert.Equal(4, session.State.Shots.Count);
        Assert.Contains(session.State.Shots, s => s.Id == moved && s.Provenance == ShotProvenance.Corrected);
        Assert.Contains(session.State.Shots, s => s.Id == byHand && s.Provenance == ShotProvenance.Manual);
        Assert.Single(session.State.Shots, s => Math.Abs(s.Image.X - 210) < 5);
    }

    [Fact]
    public void AProposalAndItsDoubtAreKeptInTheMarkingFile()
    {
        var session = Marked();
        session.ProposeHoles(Proposals);

        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State, null));
        Assert.Equal(session.State.Shots.Select(s => s.Proposal), read.Shots.Select(s => s.Proposal));
        Assert.Single(ReviewQueue.MarksToCheck(read));
    }

    [Fact]
    public void ItIsOfferedOnlyWhereTheScaleWasSetByHandAndSaysItIsExperimental()
    {
        Assert.Contains("(Experimental)", FindHoles.Label, StringComparison.Ordinal);
        Assert.StartsWith("Experimental", FindHoles.Explanation, StringComparison.Ordinal);

        var session = new MarkingSession();
        session.Open("target.png");
        Assert.False(FindHoles.Offered(session.State));
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        Assert.True(FindHoles.Offered(session.State));

        // A GroupLab sheet read from its markers has its holes found by the sheet's own detector.
        var mapping = new HomographyMapping(new Homography([2.54, 0, 0, 0, 2.54, 0, 0, 0, 1]));
        session.SetScale(new SheetReference(mapping, "registered"));
        Assert.False(FindHoles.Offered(session.State));
    }

    [Fact]
    public void TheScaleGivesThePixelsAnInch()
    {
        Assert.Equal(100, FindHoles.PixelsPerInch(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), new PointD(50, 50)), 6);
        Assert.Equal(250, FindHoles.PixelsPerInch(new LengthReference(new PointD(0, 0), new PointD(0, 500), 2), new PointD(0, 0)), 6);
    }
}
