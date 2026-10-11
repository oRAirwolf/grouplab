using System.Collections.Immutable;
using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>NOTES-FROM-PLANNING.md entry 407 section 2.1: "How did you shoot this sheet?", three answers.</summary>
public enum ShotPattern
{
    /// <summary>"One shot on each bull I used": one-to-one matching, the default.</summary>
    OnePerBull,

    /// <summary>"Some bulls took two or more": the bulls tapped take two, the others one.</summary>
    SomeTookMore,

    /// <summary>"Every hole to its nearest bull, however many": nothing is matched and nothing asked.</summary>
    Nearest,
}

/// <summary>NOTES-FROM-PLANNING.md entry 407 section 2.2: "Loads on this sheet", four answers.</summary>
public enum LoadLayout
{
    OneLoad,
    OneARow,
    OneAColumn,
    PickBulls,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 2 (concept 1A, Alan's pick): the Groups section said in two plain parts, how the sheet was shot and
/// which loads are on it, and one line reading back what GroupLab will make of it. Unholy found "Shots per bull", "Its row", "Its column",
/// "Load on the chosen bulls" and "Bulls you fired at" unclear; the rule underneath (<see cref="AssignmentRule"/>) and the subgroups
/// (<see cref="SubgroupMap"/>) are unchanged, so every saved session reads as it did.
/// <para>
/// <b>The bulls fired at</b> feed the whole-sheet offset (entry 130 section 3.1), so they are never lost. Left to GroupLab, they are the
/// bulls the whole-sheet reading puts a shot on (entry 229 section 4), and where the holes fit more than one set of bulls equally well the
/// "Which bulls did you fire at?" question is asked (entry 376 section A2): that is what "found by themselves" means. Tapping a bull makes the
/// set the person's own, with a rule that names it, exactly as "Bulls you fired at" did; "Only the bulls I aimed at", the old fourth choice,
/// is that same tap on choice one.
/// </para>
/// </summary>
public static class SheetReading
{
    /// <summary>The three answers' words, in order, each with its line.</summary>
    public static IReadOnlyList<(ShotPattern Pattern, string Words, string Line)> Patterns { get; } =
    [
        (ShotPattern.OnePerBull, "One shot on each bull I used", "GroupLab asks about any bull that seems to hold two."),
        (ShotPattern.SomeTookMore, "Some bulls took two or more", "Then tap those bulls on the sheet."),
        (ShotPattern.Nearest, "Every hole to its nearest bull, however many", "Nothing is asked."),
    ];

    /// <summary>The four load layouts' words, in order.</summary>
    public static IReadOnlyList<(LoadLayout Layout, string Words)> Layouts { get; } =
    [
        (LoadLayout.OneLoad, "One load"),
        (LoadLayout.OneARow, "One a row"),
        (LoadLayout.OneAColumn, "One a column"),
        (LoadLayout.PickBulls, "I will pick bulls"),
    ];

    /// <summary>Which answer a rule is.</summary>
    public static ShotPattern PatternOf(AssignmentRule? rule) =>
        rule is { NearestOnly: true } ? ShotPattern.Nearest
        : rule is not null && rule.PerBull.Values.Any(n => n > 1) ? ShotPattern.SomeTookMore
        : ShotPattern.OnePerBull;

    /// <summary>The bulls a person said they fired at, or null where GroupLab finds them by itself.</summary>
    public static IReadOnlySet<int>? FiredAt(AssignmentRule? rule, IReadOnlyList<BullAim> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        if (rule is null || rule.NearestOnly)
        {
            return null;
        }

        if (rule.Aimed is { } aimed)
        {
            return aimed;
        }

        // An older rule names its bulls by their counts: a nought means a bull nobody aimed at.
        return rule.PerBull.Values.Any(n => n == 0) ? bulls.Where(b => b.Scoring && rule.For(b.Index) > 0).Select(b => b.Index).ToHashSet() : null;
    }

    /// <summary>The bulls that took two or more.</summary>
    public static IReadOnlySet<int> Doubled(AssignmentRule? rule) =>
        rule is null || rule.NearestOnly ? new HashSet<int>() : rule.PerBull.Where(p => p.Value > 1).Select(p => p.Key).ToHashSet();

    /// <summary>
    /// The rule for an answer, the bulls the person said they fired at (null for GroupLab to find them) and the bulls that took two. Choice one
    /// with nothing said is no rule at all, which is today's default and lets the whole-sheet reading run.
    /// </summary>
    public static AssignmentRule? Rule(ShotPattern pattern, IReadOnlyList<BullAim> bulls, IReadOnlySet<int>? firedAt, IReadOnlySet<int>? doubled)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        if (pattern == ShotPattern.Nearest)
        {
            return AssignmentRule.Nearest;
        }

        var two = pattern == ShotPattern.SomeTookMore ? doubled ?? new HashSet<int>() : new HashSet<int>();
        if (firedAt is null && two.Count == 0)
        {
            return null;
        }

        var scoring = bulls.Where(b => b.Scoring).Select(b => b.Index).ToList();
        if (firedAt is null)
        {
            // Two on the bulls tapped and one on every other bull, as "Two on the bulls named" read: no bull is closed.
            return new AssignmentRule(false, two.ToImmutableDictionary(b => b, _ => 2));
        }

        var aimed = firedAt.Union(two).ToImmutableHashSet();
        var perBull = scoring.ToImmutableDictionary(b => b, b => two.Contains(b) ? 2 : aimed.Contains(b) ? 1 : 0);
        return new AssignmentRule(false, perBull) { Aimed = aimed };
    }

    /// <summary>
    /// "Wrong? Tap them": a bull tapped goes into the bulls fired at, or out of them. The first tap makes the set the person's own, starting from
    /// the bulls GroupLab found; the answer to "How did you shoot this sheet?" stays as it was, unless it was nearest bull, which has no bulls
    /// fired at and becomes choice one.
    /// </summary>
    public static AssignmentRule? TapFiredAt(MarkingState state, int bull)
    {
        ArgumentNullException.ThrowIfNull(state);
        var unused = Unused(state).ToHashSet();
        var set = (FiredAt(state.Rule, state.Bulls) ?? state.Bulls.Where(b => b.Scoring && !unused.Contains(b.Index)).Select(b => b.Index).ToHashSet()).ToHashSet();
        if (!set.Remove(bull))
        {
            set.Add(bull);
        }

        var pattern = PatternOf(state.Rule);
        return Rule(pattern == ShotPattern.Nearest ? ShotPattern.OnePerBull : pattern, state.Bulls, set, Doubled(state.Rule));
    }

    /// <summary>"Some bulls took two or more": a bull tapped takes two, or one again when it already took two.</summary>
    public static AssignmentRule? TapDoubled(MarkingState state, int bull)
    {
        ArgumentNullException.ThrowIfNull(state);
        var two = Doubled(state.Rule).ToHashSet();
        if (!two.Remove(bull))
        {
            two.Add(bull);
        }

        return Rule(ShotPattern.SomeTookMore, state.Bulls, FiredAt(state.Rule, state.Bulls), two);
    }

    /// <summary>The rule for a new answer to "How did you shoot this sheet?", keeping the bulls already said.</summary>
    public static AssignmentRule? Choose(MarkingState state, ShotPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Rule(pattern, state.Bulls, FiredAt(state.Rule, state.Bulls), Doubled(state.Rule));
    }

    /// <summary>The scoring bulls holding no shot now.</summary>
    public static IReadOnlyList<int> Unused(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var held = state.Shots.Where(s => s.IsShot && s.Bull is not null).Select(s => s.Bull!.Value).ToHashSet();
        return [.. state.Bulls.Where(b => b.Scoring && !held.Contains(b.Index)).Select(b => b.Index)];
    }

    /// <summary>The line under the three answers: how many bulls are unused, and whether GroupLab found them or the person said.</summary>
    public static string UnusedLine(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Rule is { NearestOnly: true })
        {
            return "Every hole goes to its nearest bull, so no bull is set aside.";
        }

        int unused = FiredAt(state.Rule, state.Bulls) is { } said ? state.Bulls.Count(b => b.Scoring && !said.Contains(b.Index)) : Unused(state).Count;
        string count = unused == 1 ? "1 bull unused" : string.Create(CultureInfo.InvariantCulture, $"{unused} bulls unused");
        return FiredAt(state.Rule, state.Bulls) is null
            ? $"Bulls left blank are found by themselves: {count}. Wrong? Tap them."
            : $"You said which bulls you fired at: {count}. Tap a bull to change it.";
    }

    /// <summary>The scoring bulls by row, then by place in the row, from the page as <see cref="AimedBulls.Rows"/> reads it.</summary>
    public static IReadOnlyList<IReadOnlyList<int>> Rows(IReadOnlyList<BullAim> bulls) => AimedBulls.Rows(bulls);

    /// <summary>The scoring bulls by column: the bulls at the same place in every row.</summary>
    public static IReadOnlyList<IReadOnlyList<int>> Columns(IReadOnlyList<BullAim> bulls)
    {
        var rows = AimedBulls.Rows(bulls);
        int widest = rows.Count == 0 ? 0 : rows.Max(r => r.Count);
        return [.. Enumerable.Range(0, widest).Select(c => (IReadOnlyList<int>)[.. rows.Where(r => r.Count > c).Select(r => r[c])])];
    }

    /// <summary>The layout a sheet's loads already have, so the screen opens on it: one load, one a row, one a column, or picked bulls.</summary>
    public static LoadLayout LayoutOf(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var map = state.Subgroups;
        var scoring = state.Bulls.Where(b => b.Scoring).ToList();
        if (map is null || map.ByBull.IsEmpty || scoring.Count == 0)
        {
            return LoadLayout.OneLoad;
        }

        bool Same(IEnumerable<int> group) => group.Select(map.For).Distinct().Count() == 1;
        if (scoring.All(b => map.For(b.Index) is not null) && Same(scoring.Select(b => b.Index)))
        {
            return LoadLayout.OneLoad;
        }

        if (Rows(state.Bulls) is { Count: > 1 } rows && rows.All(Same))
        {
            return LoadLayout.OneARow;
        }

        if (Columns(state.Bulls) is { Count: > 1 } columns && columns.All(Same))
        {
            return LoadLayout.OneAColumn;
        }

        return LoadLayout.PickBulls;
    }

    /// <summary>Puts a load on every bull of a group, or takes it off with null, as one step a bull.</summary>
    public static void SetLoad(MarkingSession session, IEnumerable<int> bulls, string? load)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(bulls);
        foreach (int bull in bulls)
        {
            session.SetSubgroup(bull, load);
        }
    }

    /// <summary>
    /// The read-back line: "GroupLab will read it as: 23 bulls fired at, one shot each, rows 1 to 5 as Loads A to E." It replaces what
    /// "Shots per bull" used to say under itself.
    /// </summary>
    public static string ReadBack(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var scoring = state.Bulls.Where(b => b.Scoring).ToList();
        if (scoring.Count == 0)
        {
            return "";
        }

        var rule = state.Rule;
        string shots;
        if (rule is { NearestOnly: true })
        {
            shots = "every hole to its nearest bull, however many";
        }
        else
        {
            int fired = FiredAt(rule, state.Bulls) is { } said ? said.Count : scoring.Count - Unused(state).Count;
            var two = Doubled(rule);
            string bulls = fired == 1 ? "1 bull fired at" : string.Create(CultureInfo.InvariantCulture, $"{fired} bulls fired at");
            shots = two.Count == 0 ? bulls + ", one shot each"
                : string.Create(CultureInfo.InvariantCulture, $"{bulls}, {two.Count} of them with two shots and the rest one");
        }

        return $"GroupLab will read it as: {shots}{Loads(state)}.";
    }

    /// <summary>The loads part of the read-back, starting with a comma, or empty where no bull has one.</summary>
    private static string Loads(MarkingState state)
    {
        var map = state.Subgroups;
        if (map is null || map.ByBull.IsEmpty)
        {
            return ", no load named";
        }

        var names = map.Names;
        switch (LayoutOf(state))
        {
            case LoadLayout.OneLoad:
                return ", all as " + names[0];
            case LoadLayout.OneARow:
            case LoadLayout.OneAColumn:
                bool byRow = LayoutOf(state) == LoadLayout.OneARow;
                var lines = byRow ? Rows(state.Bulls) : Columns(state.Bulls);
                var named = lines.Select((line, i) => (Number: i + 1, Load: map.For(line[0]))).Where(x => x.Load is not null).ToList();
                if (named.Count == 0)
                {
                    return ", no load named";
                }

                string which = byRow ? "row" : "column";
                string span = named.Count == 1 ? $"{which} {named[0].Number}"
                    : named.Select(n => n.Number).SequenceEqual(Enumerable.Range(named[0].Number, named.Count))
                        ? string.Create(CultureInfo.InvariantCulture, $"{which}s {named[0].Number} to {named[^1].Number}")
                        : $"{which}s " + string.Join(", ", named.Select(n => n.Number.ToString(CultureInfo.InvariantCulture)));
                var loads = named.Select(n => n.Load!).ToList();
                string as_ = loads.Distinct().Count() == 1 ? loads[0]
                    : loads.Count == 2 ? $"{loads[0]} and {loads[1]}"
                    : $"{loads[0]} to {loads[^1]}";
                return $", {span} as {as_}";
            default:
                int on = state.Bulls.Count(b => b.Scoring && map.For(b.Index) is not null);
                return string.Create(CultureInfo.InvariantCulture, $", {names.Count} load{(names.Count == 1 ? "" : "s")} on {on} bull{(on == 1 ? "" : "s")}");
        }
    }
}
