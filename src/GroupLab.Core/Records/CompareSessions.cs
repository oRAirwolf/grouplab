using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>One saved session as a group to compare: its name, its kept shots about their own bulls, a line saying what it is, and its load.</summary>
public sealed record CompareGroup(string Name, IReadOnlyList<PointD> Offsets, string Detail, string? Load);

/// <summary>
/// The groups to compare, the distance their figures are given at, what was done to put them on one footing, or why they cannot be.
/// </summary>
public sealed record CompareSetup(IReadOnlyList<CompareGroup> Groups, double? Distance, string? Footing, string? Refusal);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 258: saved sessions made into groups to compare, moved out of the desktop's Compare screen so the phone
/// compares the same shots the same way. Each session's kept shots about their own bulls, sighters left out and excluded shots counted; two
/// sessions told apart by their loads, dates and times (entry 295, SessionNames); and sessions shot at different distances scaled to the
/// first's, so they are compared as angles, or refused where one has no distance to scale by.
/// </summary>
public static class CompareSessions
{
    /// <summary>The shots that count, about their own bulls, and how many were excluded and left out.</summary>
    public static (IReadOnlyList<PointD> Offsets, int Excluded) KeptOffsets(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var shots = state.Shots.Where(s => s.IsShot && !(s.Bull is { } b && sighters.Contains(b))).ToList();
        var kept = shots.Where(s => s.Exclusion is null).ToList();
        return (GroupAnalysis.CompositeOffsets(state, kept), shots.Count - kept.Count);
    }

    /// <summary>The records as groups, oldest first as given, on one footing.</summary>
    public static CompareSetup From(IReadOnlyList<SessionRecord> records, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(units);
        var groups = new List<CompareGroup>();
        var distances = new List<double?>();
        // Entry 295 section 3: named by the load, the date and the time, as far as it takes to tell them apart, with the sheet beneath.
        var names = SessionNames.For(records);
        foreach (var (record, name) in records.Zip(names))
        {
            var state = MarkingFile.Read(record.MarkingJson).State;
            var (offsets, excluded) = KeptOffsets(state);
            string when = name.Name.Contains(name.When, StringComparison.Ordinal) ? "" : $", {name.When}";
            groups.Add(new CompareGroup(name.Name, offsets,
                string.Create(CultureInfo.InvariantCulture, $"{offsets.Count} shots{when}, {name.Sheet}{(excluded > 0 ? $", {excluded} excluded and left out" : "")}"),
                record.Load));
            distances.Add(record.DistanceInches);
        }

        double? distance = distances.FirstOrDefault();
        if (distances.Distinct().Count() <= 1)
        {
            return new CompareSetup(groups, distance, null, null);
        }

        if (distances.Any(d => d is null))
        {
            return new CompareSetup([], distance, null,
                "One of these sessions has no distance and the others differ, so they cannot be put on one footing. Set the distance on it, accept it again, and compare.");
        }

        double first = distances[0]!.Value;
        for (int i = 0; i < groups.Count; i++)
        {
            double k = first / distances[i]!.Value;
            groups[i] = groups[i] with
            {
                Offsets = [.. groups[i].Offsets.Select(o => new PointD(o.X * k, o.Y * k))],
                Detail = groups[i].Detail + $", shot at {units.DistanceText(distances[i]!.Value)}",
            };
        }

        return new CompareSetup(groups, first, $"These were shot at different distances, so they are compared as angles: every group is scaled to {units.DistanceText(first)}, where its figures are given.", null);
    }
}
