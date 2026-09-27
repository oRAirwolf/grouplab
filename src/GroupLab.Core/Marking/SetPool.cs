using System.Globalization;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Marking;

/// <summary>What pooling the sheets of one set found: which sheets are here, missing or read twice, the bulls the set holds, and the group.</summary>
public sealed record SetPoolResult(int SetSize, IReadOnlyList<int> Found, IReadOnlyList<int> Missing, IReadOnlyList<int> Repeated, int BullsInSet,
    int Shots, GroupFigures? Figures, string Said);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.1: a set of sheets from "Made for your optic" analyzed as one group. The sheets are scanned or
/// photographed in any order; each one's codes say which of the set it is (<see cref="MarkingState.SetSheet"/>), so the set knows how
/// many sheets it has and how many shots to expect, and says which sheets are still missing. The group is every shot about its own bull on
/// its own sheet, docs/STATISTICS.md's rule for groups shot at separate aim points. A sheet read twice counts once.
/// </summary>
public static class SetPool
{
    public static SetPoolResult Pool(TargetDefinition definition, IReadOnlyList<MarkingState> sheets)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(sheets);
        int size = definition.Tiling is { } set ? set.Cols * set.Rows : 1;
        int bulls = definition.Bulls.Count(b => b.Scoring) * size;
        var read = sheets.Where(s => s.SetSheet is not null).ToList();
        var found = read.Select(s => s.SetSheet!.Value).Distinct().Order().ToList();
        var repeated = read.GroupBy(s => s.SetSheet!.Value).Where(g => g.Count() > 1).Select(g => g.Key).Order().ToList();
        var missing = Enumerable.Range(0, size).Except(found).ToList();
        var once = read.GroupBy(s => s.SetSheet!.Value).Select(g => g.First()).ToList();
        var figures = GroupAnalysis.Pooled(once);
        int shots = figures?.Shots ?? 0;
        return new SetPoolResult(size, found, missing, repeated, bulls, shots, figures, Words(size, found, missing, repeated, bulls, shots));
    }

    private static string Words(int size, IReadOnlyList<int> found, IReadOnlyList<int> missing, IReadOnlyList<int> repeated, int bulls, int shots)
    {
        static string List(IEnumerable<int> sheets)
        {
            var n = sheets.Select(s => (s + 1).ToString(CultureInfo.InvariantCulture)).ToList();
            return n.Count switch
            {
                0 => "",
                1 => n[0],
                _ => string.Join(", ", n.Take(n.Count - 1)) + " and " + n[^1],
            };
        }

        string said = found.Count == 0
            ? string.Create(CultureInfo.InvariantCulture, $"None of the {size} sheets of this set has been read yet.")
            : missing.Count == 0
                ? string.Create(CultureInfo.InvariantCulture, $"All {size} sheets of the set are here.")
                : string.Create(CultureInfo.InvariantCulture, $"Sheet{(found.Count == 1 ? "" : "s")} {List(found)} of {size} {(found.Count == 1 ? "is" : "are")} here; {List(missing)} {(missing.Count == 1 ? "is" : "are")} still missing.");
        said += string.Create(CultureInfo.InvariantCulture, $" {shots} shot{(shots == 1 ? "" : "s")} pooled, of the {bulls} bulls the set holds, one shot to a bull.");
        if (repeated.Count > 0)
        {
            said += string.Create(CultureInfo.InvariantCulture, $" Sheet{(repeated.Count == 1 ? "" : "s")} {List(repeated)} {(repeated.Count == 1 ? "was" : "were")} read more than once and count{(repeated.Count == 1 ? "s" : "")} once.");
        }

        return said;
    }
}
