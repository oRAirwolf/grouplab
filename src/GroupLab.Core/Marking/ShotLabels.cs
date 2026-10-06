namespace GroupLab.Core.Marking;

/// <summary>
/// What a shot is called wherever it appears, and <see cref="Abnormal"/> when that name marks a case needing a decision: a bull holding more
/// than one shot, a shot with no bull on a sheet of bulls, or a mark that is not a shot. <see cref="Text"/> is null on a marking with no
/// bulls at all, a plain group, where there is no printed number to name a shot by and the screen identifies it by position instead.
/// </summary>
public sealed record ShotLabel(int ShotId, string? Text, bool Abnormal)
{
    /// <summary>
    /// Entry 376 section A6: the shot's name in words, by its bull: "Bull 7", or "Bull 7, shot 2" where the bull holds more than one, so
    /// no list shows a bare number that reads as a count. "Unassigned shot" and "Not a shot" for the others; null on a plain group.
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// One numbering system, the bull's, NOTES-FROM-PLANNING.md entry 75. A shot is named by the bull it sits on, the number printed beside that
/// bull on the paper, in the panel, on the canvas and in anything exported. A number beside a hole reads as the order the shots were fired,
/// which the detector cannot know, so no per-detection index is shown anywhere.
/// <list type="bullet">
/// <item>One shot on a bull is that bull's label: <c>7</c>, or <c>S1</c> for a sighter.</item>
/// <item>More than one shot on a bull is <c>7a</c>, <c>7b</c>, lettered by position on the image, top to bottom and then left to right, so
/// the letters never depend on the order detection happened to emit the holes, and never look like another bull's number.</item>
/// <item>A shot with no bull on a sheet of bulls is <see cref="Unassigned"/>, a word rather than a number, and a mark that is not a shot is
/// <see cref="NotAShot"/>.</item>
/// </list>
/// The order is the sheet's: each bull in the definition's order, scoring bulls before sighters, then unassigned shots, then marks that are
/// not shots. On a marking with no bulls the shots are ordered by position.
/// </summary>
public static class ShotLabels
{
    public const string Unassigned = "unassigned";

    public const string NotAShot = "not a shot";

    public static IReadOnlyList<ShotLabel> For(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        static IEnumerable<MarkedShot> ByPosition(IEnumerable<MarkedShot> shots) => shots.OrderBy(s => s.Image.Y).ThenBy(s => s.Image.X).ThenBy(s => s.Id);
        var shots = state.Shots.Where(s => s.IsShot).ToList();
        var labels = new List<ShotLabel>(state.Shots.Count);
        if (state.Bulls.Count == 0)
        {
            labels.AddRange(ByPosition(shots).Select(s => new ShotLabel(s.Id, null, false)));
        }
        else
        {
            var bulls = state.Bulls.Where(b => b.Scoring).Concat(state.Bulls.Where(b => !b.Scoring)).ToList();
            var known = bulls.Select(b => b.Index).ToHashSet();
            foreach (var bull in bulls)
            {
                var on = ByPosition(shots.Where(s => s.Bull == bull.Index)).ToList();
                labels.AddRange(on.Select((s, k) => on.Count == 1
                    ? new ShotLabel(s.Id, bull.Label, false) { Name = "Bull " + bull.Label }
                    : new ShotLabel(s.Id, bull.Label + Letters(k), true) { Name = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Bull {bull.Label}, shot {k + 1}") }));
            }

            labels.AddRange(ByPosition(shots.Where(s => s.Bull is not { } b || !known.Contains(b))).Select(s => new ShotLabel(s.Id, Unassigned, true) { Name = "Unassigned shot" }));
        }

        labels.AddRange(ByPosition(state.Shots.Where(s => !s.IsShot)).Select(s => new ShotLabel(s.Id, NotAShot, true) { Name = "Not a shot" }));
        return labels;
    }

    /// <summary>
    /// Entry 376 section A6: the extreme spread's two shots in words, "bulls 1 and 15" when each is alone on its bull, the shots' names
    /// otherwise, so the legend never names a shot by a bare number.
    /// </summary>
    public static string Pair(ShotLabel? a, ShotLabel? b)
    {
        if (a?.Name is null || b?.Name is null)
        {
            return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"shots {a?.Text ?? "?"} and {b?.Text ?? "?"}");
        }

        return !a.Abnormal && !b.Abnormal ? $"bulls {a.Text} and {b.Text}" : $"{InSentence(a.Name)} and {InSentence(b.Name)}";
    }

    /// <summary>
    /// A shot's name inside a sentence, so no sentence reads "bull 3 is now on bull 4": "the shot on bull 7", "shot 2 on bull 7", "an
    /// unassigned shot", "a mark that is not a shot".
    /// </summary>
    public static string InSentence(string name)
    {
        var two = System.Text.RegularExpressions.Regex.Match(name, "^Bull (.+), shot ([0-9]+)$");
        return two.Success ? $"shot {two.Groups[2].Value} on bull {two.Groups[1].Value}"
            : name.StartsWith("Bull ", StringComparison.Ordinal) ? "the shot on bull " + name[5..]
            : name == "Unassigned shot" ? "an unassigned shot"
            : name == "Not a shot" ? "a mark that is not a shot"
            : name.Length > 0 ? char.ToLowerInvariant(name[0]) + name[1..] : name;
    }

    /// <summary><see cref="InSentence"/> at the start of one: "The shot on bull 7".</summary>
    public static string Start(string name)
    {
        string said = InSentence(name);
        return said.Length > 0 ? char.ToUpperInvariant(said[0]) + said[1..] : said;
    }

    /// <summary>a to z, then aa, ab and on, for the k-th shot on one bull counting from zero.</summary>
    private static string Letters(int k) => k < 26 ? ((char)('a' + k)).ToString() : Letters((k / 26) - 1) + (char)('a' + (k % 26));
}
