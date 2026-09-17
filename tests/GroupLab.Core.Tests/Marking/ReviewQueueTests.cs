using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 83 section 4 and DESIGN.md section 13: the assignment editor's review queue, what it lists, in what order, how
/// a choice settles an item, and that a person's "keep it" survives a save.
/// </summary>
public class ReviewQueueTests
{
    private static readonly LengthReference Scale = new(new PointD(0, 0), new PointD(100, 0), 1);

    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(100, 100)), new(1, "2", new PointD(250, 100)), new(2, "3", new PointD(400, 100)), new(3, "S1", new PointD(250, 300), Scoring: false)];

    private static MarkingSession Loaded(params (PointD Image, AssignedShot Assignment, DetectedOversize? Oversize)[] shots)
    {
        var session = new MarkingSession();
        session.Open("sheet.png");
        var result = new ShotAssignmentResult(AssignmentMethod.OneToOne, "test", [.. shots.Select(s => s.Assignment)]);
        var rejected = new[] { new RejectedCandidate(new PointD(405, 110), 0.12, "too small, 0.120 in"), new RejectedCandidate(new PointD(400, 90), 0.30, "not compact, hull solidity 0.30") };
        session.LoadDetections(Scale, Bulls, [.. shots.Select(s => new DetectedShot(s.Image, s.Assignment, 0.3, s.Oversize))], result, rejected, "test");
        return session;
    }

    [Fact]
    public void TheQueueListsEachKindInOrderWithItsChoices()
    {
        // Shot 1 on bull 1; shot 2 nearest bull 1 but matched to bull 2; shot 3 oversized on bull 2 as well; bull 3 empty with a small refused candidate.
        var session = Loaded(
            (new PointD(110, 100), new AssignedShot(0, 0, 25, 0, 25, 3000, false), null),
            (new PointD(160, 100), new AssignedShot(1, 1, 229, 0, 152, 229, false), null),
            (new PointD(260, 105), new AssignedShot(2, 1, 28, 1, 28, 3000, false), new DetectedOversize(1.8, false)));
        var items = ReviewQueue.For(session.State);

        Assert.Equal([ReviewKind.Contested, ReviewKind.Oversized, ReviewKind.Doubled, ReviewKind.Refused], items.Select(i => i.Kind));
        Assert.Equal(4, ReviewQueue.Open(items));
        var contested = items[0];
        Assert.Contains("Nearest bull says 1, but bull 1 already holds shot 1", contested.Sentence, StringComparison.Ordinal);
        Assert.Equal([(ReviewAction.AssignBull, (int?)1), (ReviewAction.AssignBull, 0), (ReviewAction.NotAShot, null)], contested.Choices.Select(c => (c.Action, c.Bull)));
        Assert.Contains("Bull 3 has no shot. A 0.12 in candidate", items[3].Sentence, StringComparison.Ordinal);

        // A choice settles its item and only its item.
        ReviewQueue.Apply(session, contested, contested.Choices[1]);
        items = ReviewQueue.For(session.State);
        Assert.True(items.Single(i => i.Kind == ReviewKind.Contested).Resolved);
        Assert.False(items.Single(i => i.Kind == ReviewKind.Oversized).Resolved);

        // Keeping the oversized mark and adding the refused candidate settle both, and the added shot is a hand-placed one on bull 3.
        ReviewQueue.Apply(session, items.Single(i => i.Kind == ReviewKind.Oversized), items.Single(i => i.Kind == ReviewKind.Oversized).Choices[0]);
        var refused = items.Single(i => i.Kind == ReviewKind.Refused);
        int added = ReviewQueue.Apply(session, refused, refused.Choices[0])!.Value;
        Assert.Equal((ShotProvenance.Manual, (int?)2), (session.State.Find(added)!.Provenance, session.State.Find(added)!.Bull));
        items = ReviewQueue.For(session.State);
        Assert.All(items.Where(i => i.Kind is ReviewKind.Contested or ReviewKind.Oversized or ReviewKind.Refused), i => Assert.True(i.Resolved));
    }

    [Fact]
    public void AKeptItemIsRememberedAcrossASaveAndUndoTakesItBack()
    {
        var session = Loaded((new PointD(260, 105), new AssignedShot(0, 1, 28, 1, 28, 3000, false), new DetectedOversize(1.8, false)));
        static ReviewItem Oversized(MarkingState state) => Assert.Single(ReviewQueue.For(state), i => i.Kind == ReviewKind.Oversized);
        var item = Oversized(session.State);
        ReviewQueue.Apply(session, item, item.Choices[0]);
        Assert.True(Oversized(session.State).Resolved);

        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.True(Oversized(read).Resolved);

        session.Undo();
        Assert.False(Oversized(session.State).Resolved);
    }

    [Fact]
    public void AShotWithNoBullOffersItsNearestAndAPlainMarkingHasNoQueue()
    {
        var session = new MarkingSession();
        session.Open("group.jpg");
        session.SetScale(Scale);
        Assert.Empty(ReviewQueue.For(session.State));

        session = Loaded((new PointD(110, 100), new AssignedShot(0, 0, 25, 0, 25, 3000, false), null));
        int id = session.State.Shots[0].Id;
        session.AssignBull(id, null);
        var item = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Unassigned);
        Assert.Equal((ReviewAction.AssignBull, (int?)0), (item.Choices[0].Action, item.Choices[0].Bull));
        ReviewQueue.Apply(session, item, item.Choices[0]);
        Assert.Equal(0, session.State.Find(id)!.Bull);
    }
}
