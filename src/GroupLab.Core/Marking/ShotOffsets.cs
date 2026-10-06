namespace GroupLab.Core.Marking;

/// <summary>
/// One shot's row of the offsets table: its label, whether it was left out, its offset from its own aim point in inches at the target, right
/// and up positive, and the clicks that would bring it onto the aim where a scope's click value and the distance are known.
/// </summary>
public sealed record ShotOffsetRow(int ShotId, string Label, bool LeftOut, double AcrossInches, double UpInches, Clicks? AcrossClicks, Clicks? UpClicks);

/// <summary>
/// The table of each shot's offset from the aim point, NOTES-FROM-PLANNING.md entry 278 section 5f: across and up and down, with scope clicks
/// beside them from the rifle's click value, in the chosen unit where no scope is set (the screen converts the inches). A shot left out is in
/// the table, marked, because leaving it out of the figures is not deleting it (entry 278 section 5c). Sighters and marks that are not
/// shots are not in it. The table's layout waits for planning's concept; this is its data.
/// </summary>
public static class ShotOffsets
{
    public static IReadOnlyList<ShotOffsetRow> Table(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var shots = state.Shots.Where(s => s.IsShot && !GroupAnalysis.OnSighter(state, s)).ToList();
        if (shots.Count == 0 || state.Scale is null || !GroupAnalysis.HasOrigin(state, shots))
        {
            return [];
        }

        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        if (offsets.Count != shots.Count)
        {
            return [];
        }

        // Entry 376 section A6: each row named by its bull, "Bull 1" or "Bull 1, shot 2", in bull order, lowest first.
        var named = ShotLabels.For(state);
        var labels = named.ToDictionary(l => l.ShotId, l => l.Name);
        var place = named.Select((l, k) => (l.ShotId, k)).ToDictionary(x => x.ShotId, x => x.k);
        var rifle = state.Rifle;
        double? distance = state.ShotDistanceInches;
        var rows = new List<ShotOffsetRow>();
        for (int i = 0; i < shots.Count; i++)
        {
            // The composite offsets are on the screen's axes, right and down positive; the table reads right and up.
            double across = offsets[i].X, up = -offsets[i].Y;
            Clicks? Dial(double inches, string towardPositive, string towardNegative) =>
                rifle is not null && distance is > 0 && inches != 0 ? Clicks.For(inches, distance.Value, rifle, inches > 0 ? towardNegative : towardPositive) : null;
            rows.Add(new ShotOffsetRow(
                shots[i].Id,
                labels.GetValueOrDefault(shots[i].Id) ?? "Shot " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                shots[i].Exclusion is not null,
                across,
                up,
                Dial(across, "right", "left"),
                Dial(up, "up", "down")));
        }

        return state.Bulls.Count == 0 ? rows : [.. rows.OrderBy(r => place.GetValueOrDefault(r.ShotId, int.MaxValue))];
    }
}
