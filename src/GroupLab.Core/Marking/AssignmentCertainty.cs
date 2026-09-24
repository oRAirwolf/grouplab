using System.Globalization;
using GroupLab.Core.Detection;

namespace GroupLab.Core.Marking;

/// <summary>Why an analysis is resting on something nobody has confirmed, and what that means for the figures built on it.</summary>
/// <param name="Certain">Whether the figures can be presented as settled.</param>
/// <param name="Says">One line for the screen and for a report, or empty when there is nothing to say.</param>
/// <param name="Shots">The shots a person should look at, by id.</param>
public sealed record AssignmentCertainty(bool Certain, string Says, IReadOnlyList<int> Shots)
{
    /// <summary>Nothing in doubt.</summary>
    public static AssignmentCertainty Settled { get; } = new(true, "", []);
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 3.3: when the assignment is uncertain, <b>every figure built on it is uncertain</b>, and says so
/// until the shooter has looked.
/// <para>
/// <b>Why this is a separate thing rather than a flag somewhere.</b> Entry 120's third point was that GroupLab already says an assignment is
/// contested and already lets a hole be moved, and then presents the group size, the composite and the zero correction as though none of
/// that had happened. A person reads the figures. They do not read the queue, and they have no reason to think the two are connected.
/// </para>
/// <para>
/// So the doubt has to travel with the number. A mean radius computed from shots that may belong to other bulls is not a slightly worse mean
/// radius; it is a measurement of something nobody has established. A zero correction from it tells somebody to dial a rifle that may not
/// need dialling, and that is the one outcome worth preventing at any cost in tidiness.
/// </para>
/// </summary>
public static class AssignmentCertainties
{
    /// <summary>
    /// What the state of a marking means for the figures: settled, or uncertain with the reason and the shots to look at.
    /// </summary>
    /// <param name="items">The review queue for this marking.</param>
    /// <param name="offsets">The point of impact found per subgroup, where one was found.</param>
    public static AssignmentCertainty Of(IReadOnlyList<ReviewItem> items, IReadOnlyList<ImpactOffset>? offsets = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        // A contested shot is one the matching gave a bull other than its nearest, or whose margin is small enough that a registration
        // error would flip it. Either way the figures rest on a guess.
        var contested = items
            .Where(i => !i.Resolved && i.Kind is ReviewKind.Contested or ReviewKind.Unassigned)
            .Select(i => i.ShotId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var unsettled = (offsets ?? []).Where(o => !o.Certain).ToList();

        if (contested.Count == 0 && unsettled.Count == 0)
        {
            return AssignmentCertainty.Settled;
        }

        var inv = CultureInfo.InvariantCulture;
        var reasons = new List<string>();

        if (contested.Count > 0)
        {
            reasons.Add(contested.Count == 1
                ? "one shot could belong to more than one bull"
                : string.Create(inv, $"{contested.Count} shots could belong to more than one bull"));
        }

        if (unsettled.Count > 0)
        {
            reasons.Add(unsettled.Count == 1
                ? "where one group of shots was aimed did not settle"
                : string.Create(inv, $"where {unsettled.Count} groups of shots were aimed did not settle"));
        }

        string says = "These figures are not settled: " + string.Join(", and ", reasons)
            + ". Until that is reviewed, the group size, the composite and the zero correction are all measuring something nobody has "
            + "confirmed. Settle the review queue and they become figures you can act on.";

        return new AssignmentCertainty(false, says, contested);
    }

    /// <summary>
    /// What a single figure carries beside it while the assignment is unsettled. Short, because it sits next to a number rather than
    /// instead of it: the full reason is <see cref="AssignmentCertainty.Says"/>.
    /// </summary>
    public const string BesideAFigure = "not settled";

    /// <summary>
    /// The zero correction is the one that changes what somebody does to their rifle, so it refuses rather than qualifies. A group whose
    /// shots may belong to other bulls has no point of impact to correct from, and a wrong correction is worse than none.
    /// </summary>
    public const string ZeroWithheld =
        "No zero correction while the assignment is unsettled. It would be worked out from shots that may belong to other bulls, and dialing "
        + "a rifle on that is worse than not dialing it at all. Settle the review queue first.";
}
