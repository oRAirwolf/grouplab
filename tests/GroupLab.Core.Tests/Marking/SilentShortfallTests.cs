using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.2, from what entry 120 found on the second range day.
/// <para>
/// <b>The harm, exactly.</b> Alan fired fifteen at scan 1. GroupLab found fourteen, put nothing in the review queue, and presented the
/// figures as a clean result. He had no reason to look, and a shooter who does not look at a group of fourteen that should be fifteen is
/// being told something false about their rifle. It was the same bull as the hole missed on his first sheet the day before.
/// </para>
/// <para>
/// The cause was narrow and worth naming: the count check only ran when somebody had <b>typed</b> a number. Nobody had, because the sheet
/// already said it. Analysing one shot to a bull across fifteen scoring bulls is a statement that fifteen were fired, and it was being
/// ignored. These hold that the sheet's own arithmetic counts, and that the shortfall names where to look.
/// </para>
/// </summary>
public class SilentShortfallTests
{
    private static readonly LengthReference Scale = new(new PointD(0, 0), new PointD(254, 0), 1);

    /// <summary>A generated sheet: five scoring bulls in a row, one sighter, nothing from any scan.</summary>
    private static BullAim[] Sheet(int scoring = 5) =>
    [
        .. Enumerable.Range(0, scoring).Select(i => new BullAim(i, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(100 + (i * 150), 100))),
        new(scoring, "S1", new PointD(250, 400), Scoring: false),
    ];

    private static MarkingSession Loaded(BullAim[] bulls, params (int Bull, PointD Image)[] shots)
    {
        var session = new MarkingSession();
        session.Open("generated.png");
        var assigned = shots.Select((s, i) => new AssignedShot(i, s.Bull, 10, s.Bull, 10, 3000, false)).ToList();
        var result = new ShotAssignmentResult(AssignmentMethod.OneToOne, "generated", assigned);
        session.LoadDetections(Scale, bulls, [.. shots.Select((s, i) => new DetectedShot(s.Image, assigned[i], 0.22, null))], result, [], "generated");
        return session;
    }

    /// <summary>
    /// The fault itself: a sheet of five bulls with four shots on it, nobody having typed a count. Before this, the queue was empty.
    /// </summary>
    [Fact]
    public void AShortfallIsRaisedEvenWhenNobodyTypedACount()
    {
        var bulls = Sheet();
        var session = Loaded(bulls,
            (0, new PointD(100, 100)),
            (1, new PointD(250, 100)),
            (2, new PointD(400, 100)),
            (3, new PointD(550, 100)));

        Assert.Null(session.State.ExpectedShots);
        Assert.Equal(5, ReviewQueue.Expected(session.State));

        var shortfall = ReviewQueue.For(session.State).SingleOrDefault(i => i.Kind == ReviewKind.Count);
        Assert.NotNull(shortfall);
        Assert.False(shortfall.Resolved);
        Assert.Contains("You fired 5 and 4 are marked", shortfall.Sentence, StringComparison.Ordinal);
    }

    /// <summary>It names where to look, which is the difference between a warning and a useful one.</summary>
    [Fact]
    public void TheShortfallNamesTheBullsWithNothingOnThem()
    {
        var bulls = Sheet();
        var session = Loaded(bulls,
            (0, new PointD(100, 100)),
            (2, new PointD(400, 100)),
            (4, new PointD(700, 100)));

        var shortfall = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Count);

        Assert.Contains("You fired 5 and 3 are marked", shortfall.Sentence, StringComparison.Ordinal);
        Assert.Contains("Nothing is marked on bulls 2, 4", shortfall.Sentence, StringComparison.Ordinal);
    }

    /// <summary>One missing bull reads as one, because "bulls 3" is the sort of thing that makes a person distrust the rest.</summary>
    [Fact]
    public void OneEmptyBullIsNamedInTheSingular()
    {
        var bulls = Sheet(3);
        var session = Loaded(bulls, (0, new PointD(100, 100)), (1, new PointD(250, 100)));

        var shortfall = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Count);
        Assert.Contains("Nothing is marked on bull 3.", shortfall.Sentence, StringComparison.Ordinal);
    }

    /// <summary>A full sheet raises nothing, so this cannot be passing by warning about everything.</summary>
    [Fact]
    public void AFullSheetRaisesNoShortfall()
    {
        var bulls = Sheet();
        var session = Loaded(bulls,
            (0, new PointD(100, 100)),
            (1, new PointD(250, 100)),
            (2, new PointD(400, 100)),
            (3, new PointD(550, 100)),
            (4, new PointD(700, 100)));

        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }

    /// <summary>
    /// Sighters are not scoring, so they are not part of the count either way. A shot on a sighter does not make up a shortfall, and a
    /// sighter with nothing on it is not a missing shot.
    /// </summary>
    [Fact]
    public void SightersAreNotCounted()
    {
        var bulls = Sheet(2);
        var session = Loaded(bulls,
            (0, new PointD(100, 100)),
            (1, new PointD(250, 100)),
            (2, new PointD(250, 400)));

        Assert.Equal(2, ReviewQueue.Expected(session.State));
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }

    /// <summary>
    /// Nearest-bull is the person saying they are not counting: a plain sheet, or one where the bulls hold whatever they hold. Inventing a
    /// count there would be GroupLab telling somebody they had lost a shot they never fired.
    /// </summary>
    [Fact]
    public void NearestBullMeansNoCountIsClaimed()
    {
        var bulls = Sheet();
        var session = Loaded(bulls, (0, new PointD(100, 100)), (1, new PointD(250, 100)));
        session.SetAssignmentRule(AssignmentRule.Nearest);

        Assert.Null(ReviewQueue.Expected(session.State));
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }

    /// <summary>Two shots to a bull is still a count, and the arithmetic follows the rule rather than the number of bulls.</summary>
    [Fact]
    public void BullsTakingTwoShotsEachAreCountedAsTwo()
    {
        var bulls = Sheet(3);
        var session = Loaded(bulls, (0, new PointD(100, 100)), (1, new PointD(250, 100)), (2, new PointD(400, 100)));
        session.SetAssignmentRule(new AssignmentRule(false,
            System.Collections.Immutable.ImmutableDictionary<int, int>.Empty.Add(0, 2).Add(1, 2).Add(2, 2)));

        Assert.Equal(6, ReviewQueue.Expected(session.State));
        var shortfall = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Count);
        Assert.Contains("You fired 6 and 3 are marked", shortfall.Sentence, StringComparison.Ordinal);
    }

    /// <summary>A number somebody typed still wins, because they know what they fired and the sheet only knows what it was printed for.</summary>
    [Fact]
    public void ATypedCountWinsOverTheSheetsArithmetic()
    {
        var bulls = Sheet();
        var session = Loaded(bulls, (0, new PointD(100, 100)), (1, new PointD(250, 100)));
        session.SetExpectedShots(2);

        Assert.Equal(2, ReviewQueue.Expected(session.State));
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }
}
