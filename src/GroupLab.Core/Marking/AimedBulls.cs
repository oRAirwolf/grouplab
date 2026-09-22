using System.Collections.Immutable;

namespace GroupLab.Core.Marking;

/// <summary>
/// Which bulls a person aimed at, and how many shots each was given, NOTES-FROM-PLANNING.md entry 141 section 5.3.4 and question 37.
/// <para>
/// <b>This is the one thing the shooter knows and the image cannot show.</b> A sheet of twenty five bulls with twenty holes on it does not
/// say whether five bulls were missed, five were never fired at, or every shot landed a bull away from where it was aimed. GroupLab has been
/// guessing, and on a rifle that is not zeroed for the load it guesses wrong on every shot: entry 120's 6 ARC sheet put all twenty shots
/// nearer a bull they were not aimed at, and reading it by nearest bull gives a tight group about nothing at all.
/// </para>
/// <para>
/// <b>It needs no new machinery.</b> <see cref="AssignmentRule.PerBull"/> already says how many shots a bull is expected to hold, and the
/// matching already treats a bull with no room as closed. A bull nobody aimed at is a bull expecting zero shots, so saying which bulls were
/// aimed at is saying which ones expect none.
/// </para>
/// </summary>
public static class AimedBulls
{
    /// <summary>
    /// The rule for a sheet where these bulls were aimed at, with <paramref name="each"/> shots apiece and every other scoring bull expecting
    /// none. Sighter bulls are left out of the rule entirely, because the analysis already ignores them unless it is asked not to.
    /// </summary>
    public static AssignmentRule For(IEnumerable<BullAim> bulls, IEnumerable<int> aimed, int each = 1)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(aimed);
        var wanted = aimed.ToHashSet();
        var perBull = ImmutableDictionary.CreateBuilder<int, int>();
        foreach (var bull in bulls.Where(b => b.Scoring))
        {
            perBull[bull.Index] = wanted.Contains(bull.Index) ? Math.Max(0, each) : 0;
        }

        return new AssignmentRule(false, perBull.ToImmutable());
    }

    /// <summary>Every scoring bull, <paramref name="each"/> shots apiece: the ordinary sheet, and the default.</summary>
    public static AssignmentRule EveryBull(IEnumerable<BullAim> bulls, int each = 1)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        var all = bulls.Where(b => b.Scoring).Select(b => b.Index).ToList();
        return For(bulls, all, each);
    }

    /// <summary>
    /// The scoring bulls in rows, top row first, each row left to right.
    /// <para>
    /// The rows come from where the bulls are on the page, not from their order in the definition, because a person looking at a sheet sees
    /// rows and a definition is free to list them however it likes. Two bulls are on the same row when their centres are within half the
    /// gap between rows, which is wide enough for a printing tolerance and narrow enough never to merge two rows of a real sheet.
    /// </para>
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<int>> Rows(IEnumerable<BullAim> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        var scoring = bulls.Where(b => b.Scoring).Select(b => (b.Index, At: b.Declared ?? b.Image)).OrderBy(b => b.At.Y).ToList();
        if (scoring.Count == 0)
        {
            return [];
        }

        // The gap that separates rows: the largest step in y, halved, or everything is one row.
        var steps = scoring.Zip(scoring.Skip(1), (a, b) => b.At.Y - a.At.Y).ToList();
        double apart = steps.Count > 0 ? steps.Max() / 2 : 0;
        var rows = new List<List<(int Index, Imaging.PointD At)>> { new() { scoring[0] } };
        for (int i = 1; i < scoring.Count; i++)
        {
            if (scoring[i].At.Y - scoring[i - 1].At.Y > apart && apart > 0)
            {
                rows.Add([]);
            }

            rows[^1].Add(scoring[i]);
        }

        return [.. rows.Select(r => (IReadOnlyList<int>)[.. r.OrderBy(b => b.At.X).Select(b => b.Index)])];
    }

    /// <summary>The named rows, counting from one at the top: "I shot rows 1 to 3 and left the rest".</summary>
    public static AssignmentRule RowsOf(IEnumerable<BullAim> bulls, IEnumerable<int> rows, int each = 1)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(rows);
        var list = bulls.ToList();
        var byRow = Rows(list);
        var wanted = rows.ToHashSet();
        var aimed = byRow.Where((_, i) => wanted.Contains(i + 1)).SelectMany(r => r).ToList();
        return For(list, aimed, each);
    }

    /// <summary>
    /// The same columns of every row, counting from one at the left: entry 120's 6 ARC sheet is "bulls 2 to 5 of every row", which is the
    /// pattern a person shoots when they want four shots a row and a spare column.
    /// </summary>
    public static AssignmentRule ColumnsOfEveryRow(IEnumerable<BullAim> bulls, IEnumerable<int> columns, int each = 1)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(columns);
        var list = bulls.ToList();
        var wanted = columns.ToHashSet();
        var aimed = Rows(list).SelectMany(r => r.Where((_, i) => wanted.Contains(i + 1))).ToList();
        return For(list, aimed, each);
    }

    /// <summary>The scoring bulls this rule says were aimed at, in index order.</summary>
    public static IReadOnlyList<int> Of(AssignmentRule? rule, IEnumerable<BullAim> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        var scoring = bulls.Where(b => b.Scoring).Select(b => b.Index).Order().ToList();
        return rule is null || rule.NearestOnly ? scoring : [.. scoring.Where(i => rule.For(i) > 0)];
    }

    /// <summary>
    /// What the rule says, for the screen: "20 shots at 20 bulls" or "one shot at each of 15 bulls, 10 left alone". A person has to be able
    /// to read back what they told GroupLab, because this is the input that decides what every figure afterwards is about.
    /// </summary>
    public static string Says(AssignmentRule? rule, IEnumerable<BullAim> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        var list = bulls.Where(b => b.Scoring).ToList();
        if (rule is null)
        {
            return "Every bull is expected to hold one shot.";
        }

        if (rule.NearestOnly)
        {
            return "Each shot goes to the bull it is nearest, and nothing is expected of any bull.";
        }

        var aimed = Of(rule, list);
        int shots = aimed.Sum(rule.For);
        int untouched = list.Count - aimed.Count;
        var each = aimed.Select(rule.For).Distinct().ToList();
        string apiece = each.Count == 1
            ? (each[0] == 1 ? "one shot each" : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{each[0]} shots each"))
            : "a named number of shots each";

        string tail = untouched > 0
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $", and {untouched} bull{(untouched == 1 ? "" : "s")} nobody aimed at.")
            : ".";
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{shots} shot{(shots == 1 ? "" : "s")} at {aimed.Count} bull{(aimed.Count == 1 ? "" : "s")}, {apiece}{tail}");
    }
}
