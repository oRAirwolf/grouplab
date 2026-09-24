using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 3.3, which is entry 120's third point: GroupLab already said an assignment was contested and
/// already let a hole be moved, and then presented the group size, the composite and the zero correction as though none of that had
/// happened.
/// <para>
/// A person reads the figures. They do not read the review queue, and they have no reason to think the two are connected. So the doubt has
/// to travel with the number, and the one figure that tells somebody to change their rifle has to refuse outright rather than be qualified.
/// </para>
/// </summary>
public class AssignmentCertaintyTests
{
    private static ReviewItem Item(ReviewKind kind, int? shot, bool resolved = false) =>
        new($"{kind}:{shot}", kind, shot, null, default, "test", [], resolved);

    private static ImpactOffset Offset(bool certain) =>
        new("a load", [0, 1], new Offset(10, 10), certain, certain ? "settled" : "not settled");

    [Fact]
    public void NothingInDoubtIsSettled()
    {
        var certainty = AssignmentCertainties.Of([], [Offset(true)]);

        Assert.True(certainty.Certain);
        Assert.Equal("", certainty.Says);
        Assert.Empty(certainty.Shots);
    }

    /// <summary>A resolved item is one the shooter has looked at, which is the whole point of resolving it.</summary>
    [Fact]
    public void AnItemThePersonHasSettledDoesNotKeepTheFiguresUnsettled()
    {
        var certainty = AssignmentCertainties.Of([Item(ReviewKind.Contested, 3, resolved: true)]);

        Assert.True(certainty.Certain);
    }

    [Fact]
    public void AContestedShotMakesEveryFigureUnsettled()
    {
        var certainty = AssignmentCertainties.Of([Item(ReviewKind.Contested, 3)]);

        Assert.False(certainty.Certain);
        Assert.Equal([3], certainty.Shots);
        Assert.Contains("one shot could belong to more than one bull", certainty.Says, StringComparison.Ordinal);

        // It names what is affected, because "uncertain" on its own tells a person nothing about what not to trust.
        Assert.Contains("the group size, the composite and the zero correction", certainty.Says, StringComparison.Ordinal);

        // And it says how to make it settled, rather than leaving somebody stuck with a warning.
        Assert.Contains("Settle the review queue", certainty.Says, StringComparison.Ordinal);
    }

    [Fact]
    public void SeveralContestedShotsAreCountedAndListed()
    {
        var certainty = AssignmentCertainties.Of(
        [
            Item(ReviewKind.Contested, 3),
            Item(ReviewKind.Unassigned, 7),
            Item(ReviewKind.Contested, 3),
            Item(ReviewKind.Oversized, 9),
        ]);

        Assert.False(certainty.Certain);

        // Shot 3 twice is one shot, and an oversized mark is a different question from which bull a shot belongs to.
        Assert.Equal([3, 7], certainty.Shots);
        Assert.Contains("2 shots could belong to more than one bull", certainty.Says, StringComparison.Ordinal);
    }

    /// <summary>
    /// An offset that did not settle is the scan 5 case: the whole group may have been aimed somewhere other than where it was read, so
    /// every shot in it is in doubt at once, even though no single shot looks contested.
    /// </summary>
    [Fact]
    public void AnOffsetThatDidNotSettleMakesTheFiguresUnsettledOnItsOwn()
    {
        var certainty = AssignmentCertainties.Of([], [Offset(false)]);

        Assert.False(certainty.Certain);
        Assert.Contains("where one group of shots was aimed did not settle", certainty.Says, StringComparison.Ordinal);
    }

    [Fact]
    public void BothKindsOfDoubtAreSaidTogether()
    {
        var certainty = AssignmentCertainties.Of([Item(ReviewKind.Contested, 1)], [Offset(false), Offset(false)]);

        Assert.False(certainty.Certain);
        Assert.Contains("one shot could belong to more than one bull", certainty.Says, StringComparison.Ordinal);
        Assert.Contains("where 2 groups of shots were aimed did not settle", certainty.Says, StringComparison.Ordinal);
    }

    /// <summary>
    /// The zero correction refuses rather than qualifies, and that difference is the point. Every other figure is something a person reads;
    /// the zero correction is something they act on, by turning a turret.
    /// </summary>
    [Fact]
    public void TheZeroCorrectionRefusesRatherThanCarryingAWarning()
    {
        Assert.Contains("No zero correction", AssignmentCertainties.ZeroWithheld, StringComparison.Ordinal);
        Assert.Contains("worse than not dialing it at all", AssignmentCertainties.ZeroWithheld, StringComparison.Ordinal);

        // And the short label that sits beside a number is short, because it sits beside the number rather than replacing it.
        Assert.True(AssignmentCertainties.BesideAFigure.Length <= 20);
    }
}
