using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 70 section 3, as entry 74 section 1 corrects it: matching re-solves on every edit over every shot whose
/// bull nobody chose, hand-placed ones included, a chosen bull is a constraint rather than an input, a shot the re-solve moves is shown
/// as moved, the counts rule holds live over the whole free set and says when the method changes, and undo restores the pins. The sheet is the page itself, one pixel to one dmm, with two bulls 400 dmm apart and two detected
/// shots: one close to bull 1, and one 160 dmm from bull 1 and 240 from bull 2, which one-to-one matching gives to bull 2.
/// </summary>
public class MatchingRuleTests
{
    private static readonly SheetReference Sheet = new(new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1])), "test sheet");

    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(0, 0), Declared: new PointD(0, 0)), new(1, "2", new PointD(400, 0), Declared: new PointD(400, 0))];

    /// <summary>A session with the two detections loaded as the automatic path would load them, and the ids of the near and the contested shot.</summary>
    private static (MarkingSession Session, int Near, int Contested) Loaded()
    {
        var session = LoadedWith([new(30, 0), new(160, 0)]);
        var ids = session.State.Shots.Select(s => s.Id).ToList();
        return (session, ids[0], ids[1]);
    }

    private static MarkingSession LoadedWith(PointD[] shots)
    {
        var assignment = ShotAssignment.Assign(shots, [.. Bulls.Select(b => b.Declared!.Value)]);
        var session = new MarkingSession();
        session.LoadDetections(Sheet, Bulls, [.. shots.Select((p, i) => new DetectedShot(p, assignment.Shots[i]))], assignment, [], "test");
        return session;
    }

    private static int? BullOf(MarkingSession session, int id) => session.State.Find(id)!.Bull;

    [Fact]
    public void LoadingGivesTheContestedShotToTheFreeBullAndFlagsIt()
    {
        var (session, near, contested) = Loaded();
        var review = session.State.Assignment!;

        Assert.Equal((0, 1), (BullOf(session, near), BullOf(session, contested)));
        Assert.Equal(AssignmentMethod.OneToOne, review.Method);
        var card = review.For(contested)!;
        Assert.Equal((1, 0, true), (card.Bull!.Value, card.NearestBull, card.Ambiguous));
        Assert.Equal(160 / 254.0, card.NearestInches, 9);
        Assert.Equal(240 / 254.0, card.DistanceInches, 9);
        Assert.Empty(review.Moved);
    }

    /// <summary>Item 1 and item 3: a person's reassignment holds, and the shot it pushes off its bull is shown as moved rather than silently.</summary>
    [Fact]
    public void APersonsDecisionIsKeptAndTheShotItDisplacesIsShownAsMoved()
    {
        var (session, near, contested) = Loaded();

        session.AssignBull(contested, 0);

        Assert.Equal(0, BullOf(session, contested));
        Assert.Equal(1, BullOf(session, near));
        var review = session.State.Assignment!;
        Assert.Null(review.For(contested));
        var moved = Assert.Single(review.Moved);
        Assert.Equal((near, 0, 1), (moved.ShotId, moved.DetectedBull!.Value, moved.Bull!.Value));
        Assert.Contains("no choice of yours holds", review.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Item 5: undo takes the pin away with the reassignment, so the matching is not left constrained by a decision that no longer exists.
    /// Moving a shot afterwards is a position, not a choice of bull, so it pins nothing (entry 74 section 1).
    /// </summary>
    [Fact]
    public void UndoRestoresThePinsAsWellAsTheBulls()
    {
        var (session, near, contested) = Loaded();
        session.AssignBull(contested, 0);

        session.Undo();

        Assert.Equal((0, 1), (BullOf(session, near), BullOf(session, contested)));
        Assert.Equal(ShotProvenance.Automatic, session.State.Find(contested)!.Provenance);
        Assert.False(session.State.Find(contested)!.BullChosen);
        Assert.Empty(session.State.Assignment!.Moved);

        session.MoveShot(near, new PointD(35, 0));
        Assert.Equal((0, 1), (BullOf(session, near), BullOf(session, contested)));
        Assert.False(session.State.Find(near)!.BullChosen);
        Assert.NotNull(session.State.Assignment!.For(near));
    }

    /// <summary>
    /// Entry 74 section 1, Claude Code's scenario: a missed hole added beside a detection used to take the detection's bull by fiat and push
    /// the detection away. Now both are free, the matching weighs them together, and here the detection keeps its bull and the hand-placed
    /// hole is the one given the other bull, which the queue shows against the bull it was placed with.
    /// </summary>
    [Fact]
    public void AHoleAddedByHandIsMatchedRatherThanPinnedToTheBullItSnappedTo()
    {
        var session = LoadedWith([new(10, 0)]);
        int detected = session.State.Shots[0].Id;

        int hand = session.AddShot(new PointD(60, 0));

        Assert.False(session.State.Find(hand)!.BullChosen);
        Assert.Equal((0, 1), (BullOf(session, detected), BullOf(session, hand)));
        var moved = Assert.Single(session.State.Assignment!.Moved);
        Assert.Equal((hand, 0, 1), (moved.ShotId, moved.DetectedBull!.Value, moved.Bull!.Value));

        // Choosing the bull is a different decision, and that one is pinned: now the detection is the shot that moves.
        session.AssignBull(hand, 0);
        Assert.True(session.State.Find(hand)!.BullChosen);
        Assert.Equal((1, 0), (BullOf(session, detected), BullOf(session, hand)));
        Assert.Equal(detected, Assert.Single(session.State.Assignment!.Moved).ShotId);
    }

    /// <summary>Item 2: deleting a detection frees its bull, and the untouched shot that was pushed away comes back to its nearest bull.</summary>
    [Fact]
    public void AnEditReSolvesTheUntouchedShots()
    {
        var (session, near, contested) = Loaded();

        session.DeleteShot(near);

        Assert.Equal(0, BullOf(session, contested));
        var review = session.State.Assignment!;
        Assert.Equal((1, 0), (review.For(contested)!.DetectedBull!.Value, review.For(contested)!.Bull!.Value));
        Assert.False(review.For(contested)!.Ambiguous);
        Assert.Single(review.Moved);
    }

    /// <summary>
    /// Item 4, over the whole free set as entry 74 section 1 says: a third shot placed by hand on two bulls takes the free shots above the
    /// bulls, so no matching is forced, each goes to its nearest bull, all are flagged, and the review says the method changed. Deleting the
    /// hand-placed shot puts matching back.
    /// </summary>
    [Fact]
    public void MoreFreeShotsThanFreeBullsStopsTheMatchingAndSaysSo()
    {
        var (session, near, contested) = Loaded();

        int hand = session.AddShot(new PointD(405, 0));

        var review = session.State.Assignment!;
        Assert.Equal(AssignmentMethod.NearestBull, review.Method);
        Assert.True(review.MethodChanged);
        Assert.Equal(3, review.Shots.Count);
        Assert.All(review.Shots, s => Assert.True(s.Ambiguous));
        Assert.Equal((0, 0, 1), (BullOf(session, near), BullOf(session, contested), BullOf(session, hand)));

        session.DeleteShot(hand);
        Assert.Equal(AssignmentMethod.OneToOne, session.State.Assignment!.Method);
        Assert.False(session.State.Assignment.MethodChanged);
        Assert.Equal((0, 1), (BullOf(session, near), BullOf(session, contested)));
    }

    /// <summary>A shot marked not a shot takes part in nothing: it holds no bull against the others and is not matched.</summary>
    [Fact]
    public void NotAShotIsLeftOutOfTheMatching()
    {
        var (session, near, contested) = Loaded();

        session.SetNotAShot(near, true);

        Assert.Equal(0, BullOf(session, contested));
        Assert.Null(session.State.Assignment!.For(near));
    }

    /// <summary>A marking with no detection keeps the nearest-bull rule it always had, since there is no mapping to match on.</summary>
    [Fact]
    public void AMarkingWithoutADetectionIsLeftAlone()
    {
        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), Bulls, [(new PointD(30, 0), 0), (new PointD(160, 0), 0)], "test");

        Assert.All(session.State.Shots, s => Assert.Equal(0, s.Bull));
        Assert.Null(session.State.Assignment);
    }
}
