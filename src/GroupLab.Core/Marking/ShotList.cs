using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// One row of the Shots list, NOTES-FROM-PLANNING.md entry 407 section 3 (concept 2B): the shot by its name, its bull, and where it landed from
/// that bull across and up or down and how far, in inches; null where the marking has no scale or no bull to measure from. A shot left out
/// keeps its numbers, struck through; a mark that is not a hole stays on the list at the end, so nothing leaves the record.
/// </summary>
public sealed record ShotListRow(int ShotId, string Name, string? Bull, double? AcrossInches, double? UpInches, bool LeftOut, bool NotAHole)
{
    public double? RadiusInches => AcrossInches is { } a && UpInches is { } u ? Math.Sqrt((a * a) + (u * u)) : null;
}

/// <summary>A bull a shot can be put on, as its row's list offers it: the nearest first and marked so.</summary>
public sealed record BullOption(int? Index, string Words);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 3 (concept 2B, Alan's pick): the Shots list as short rows, and the one open row's parts, shared by
/// the desktop and the phone. "Not a shot" became "Delete, not a hole" and keeps what it kept: the mark, where it came from, and Undo
/// (<see cref="MarkingSession.SetNotAShot"/>). Exclude and Restore became the Counted box.
/// </summary>
public static class ShotList
{
    /// <summary>What the bull list says for no bull at all.</summary>
    public const string NoBull = "No bull";

    /// <summary>What the firing order list says before a place is given.</summary>
    public const string NotKnown = "Not known";

    /// <summary>Every shot in the sheet's order, the marks that are not holes last.</summary>
    public static IReadOnlyList<ShotListRow> Rows(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var offsets = ShotOffsets.Table(state).ToDictionary(r => r.ShotId);
        var bulls = state.Bulls.ToDictionary(b => b.Index);
        var rows = new List<ShotListRow>();
        int number = 0;
        foreach (var label in ShotLabels.For(state))
        {
            var shot = state.Find(label.ShotId)!;
            number++;
            string name = label.Name ?? label.Text ?? "Shot " + number.ToString(CultureInfo.InvariantCulture);
            string? bull = shot.Bull is { } b && bulls.TryGetValue(b, out var aim) ? aim.Label : null;
            var at = offsets.GetValueOrDefault(shot.Id);
            rows.Add(new ShotListRow(shot.Id, name, bull, at?.AcrossInches, at?.UpInches, shot.Exclusion is not null, shot.NotAShot));
        }

        return [.. rows.Where(r => !r.NotAHole), .. rows.Where(r => r.NotAHole)];
    }

    /// <summary>The bulls a shot can be put on: its nearest first, marked "(nearest)", then the rest by distance, then no bull.</summary>
    public static IReadOnlyList<BullOption> Bulls(MarkingState state, int shotId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Find(shotId) is not { } shot || state.Bulls.Count == 0)
        {
            return [];
        }

        PointPlane plane = new(state);
        var at = plane.Of(shot.Image);
        var near = state.Bulls.OrderBy(b => plane.Away(plane.Of(b.Image), at)).ToList();
        return [.. near.Select((b, i) => new BullOption(b.Index, i == 0 ? $"{b.Label} (nearest)" : b.Label)), new BullOption(null, NoBull)];
    }

    /// <summary>"Not known", then "1st", "2nd" and so on for each hole that is a shot.</summary>
    public static IReadOnlyList<string> FiredChoices(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        int count = state.Shots.Count(s => s.IsShot);
        return [NotKnown, .. Enumerable.Range(1, count).Select(Ordinal)];
    }

    /// <summary>1st, 2nd, 3rd, 4th, 11th, 12th, 21st.</summary>
    public static string Ordinal(int n)
    {
        string tail = (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return n.ToString(CultureInfo.InvariantCulture) + tail;
    }

    /// <summary>Where a shot came from, in the open row: "Found by GroupLab", "Found by GroupLab, moved by you", or "Added by you".</summary>
    public static string From(MarkedShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Provenance switch
        {
            ShotProvenance.Automatic => "Found by GroupLab",
            ShotProvenance.Corrected => "Found by GroupLab, moved by you",
            _ => "Added by you",
        };
    }

    /// <summary>Distances as <see cref="MarkingSession.NearestBull(Imaging.PointD)"/> measures them: on the target where a scale is set.</summary>
    private readonly struct PointPlane(MarkingState state)
    {
        public Imaging.PointD Of(Imaging.PointD p) => state.Scale is { } scale ? scale.ToTarget(p) : p;

        public double Away(Imaging.PointD a, Imaging.PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
    }
}
