namespace GroupLab.Core.Marking;

/// <summary>
/// What a shot is called wherever it appears, and <see cref="Abnormal"/> when that name marks a case needing a decision: a bull holding more
/// than one shot, a shot with no bull on a sheet of bulls, or a mark that is not a shot. <see cref="Text"/> is null on a marking with no
/// bulls at all, a plain group, where there is no printed number to name a shot by and the screen identifies it by position instead.
/// </summary>
public sealed record ShotLabel(int ShotId, string? Text, bool Abnormal);

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
                    ? new ShotLabel(s.Id, bull.Label, false)
                    : new ShotLabel(s.Id, bull.Label + Letters(k), true)));
            }

            labels.AddRange(ByPosition(shots.Where(s => s.Bull is not { } b || !known.Contains(b))).Select(s => new ShotLabel(s.Id, Unassigned, true)));
        }

        labels.AddRange(ByPosition(state.Shots.Where(s => !s.IsShot)).Select(s => new ShotLabel(s.Id, NotAShot, true)));
        return labels;
    }

    /// <summary>a to z, then aa, ab and on, for the k-th shot on one bull counting from zero.</summary>
    private static string Letters(int k) => k < 26 ? ((char)('a' + k)).ToString() : Letters((k / 26) - 1) + (char)('a' + (k % 26));
}
