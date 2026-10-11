using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>Why a line from a bull to a hole is drawn as unsure, NOTES-FROM-PLANNING.md entry 407 section 1.2. More than one can hold.</summary>
[Flags]
public enum LineDoubt
{
    None = 0,

    /// <summary>The line is long for the sheet: see <see cref="BullLines.LongTimesMedian"/>.</summary>
    Long = 1,

    /// <summary>The line passes into a neighbouring bull's rings: see <see cref="BullLines.NeighbourShare"/>.</summary>
    CrossesNeighbour = 2,

    /// <summary>The matching itself was not sure of the hole: its own <see cref="ShotAssignmentDetail.Ambiguous"/>.</summary>
    MatcherUnsure = 4,
}

/// <summary>One line from a bull's center to a hole given to it, in image pixels, and whether it is drawn as unsure.</summary>
public sealed record BullLine(int Shot, int Bull, PointD From, PointD To, LineDoubt Doubt)
{
    public bool Unsure => Doubt != LineDoubt.None;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1 (concept 4A, Alan's pick): a thin line from each bull's center to every hole given to it, on the
/// analysis picture and in Fix holes, on the desktop and the phone, and the ones worth a second look drawn dashed in orange with a "?" beside
/// the hole. Both screens draw from this, so what counts as unsure is the same on both.
/// <para>
/// <b>What unsure means.</b> Three things, any one enough:
/// </para>
/// <list type="bullet">
/// <item><b>Not sure</b> is the matching's own flag and nothing new: <see cref="ShotAssignmentDetail.Ambiguous"/>, set when the hole is
/// given a bull other than its nearest, or when its two nearest bulls are within <see cref="ReviewQueue.ContestedMarginInches"/> of each
/// other, so a small registration error would flip it. It is what the review queue raises as contested.</item>
/// <item><b>Long for the sheet</b>: more than <see cref="LongTimesMedian"/> times the sheet's median line and more than
/// <see cref="LongShareOfSpacing"/> of the spacing from its bull to that bull's nearest neighbour. Measured against the sheet's own lines, so a
/// whole group that landed an inch low, every line long together, does not light every line.</item>
/// <item><b>Crosses into a neighbour</b>: the line passes within <see cref="NeighbourShare"/> of another bull's spacing of that bull's
/// center. A bull's rings fill at most half the distance to the next bull, and the ring geometry is not carried in the marking, so this is
/// the ring a little inside that half.</item>
/// </list>
/// <para>
/// A hole whose bull a person chose (<see cref="MarkedShot.BullChosen"/>), or whose contested item was dismissed, is never drawn unsure: the
/// person has already settled it, and a line that stays orange after it was answered teaches people to ignore the colour.
/// </para>
/// <para>
/// Distances are on the target plane where a scale is set, which is where a sheet's bulls are evenly spaced, and in image pixels otherwise,
/// as <see cref="MarkingSession.NearestBull(PointD)"/> measures them.
/// </para>
/// </summary>
public static class BullLines
{
    /// <summary>A line longer than this many times the sheet's median line may be long.</summary>
    public const double LongTimesMedian = 2;

    /// <summary>And must also be longer than this share of the spacing from its bull to the nearest other bull.</summary>
    public const double LongShareOfSpacing = 0.25;

    /// <summary>A line passing nearer another bull's center than this share of that bull's spacing crosses into its rings.</summary>
    public const double NeighbourShare = 0.45;

    /// <summary>Every line on the marking, one a hole that is a shot and has a bull.</summary>
    public static IReadOnlyList<BullLine> For(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var bulls = state.Bulls.ToDictionary(b => b.Index);
        PointD Plane(PointD p) => state.Scale is { } scale ? scale.ToTarget(p) : p;
        var centres = state.Bulls.ToDictionary(b => b.Index, b => Plane(b.Image));
        var spacing = state.Bulls.ToDictionary(b => b.Index, b => state.Bulls.Where(o => o.Index != b.Index)
            .Select(o => Distance(centres[b.Index], centres[o.Index])).DefaultIfEmpty(double.NaN).Min());

        var held = state.Shots.Where(s => s.IsShot && s.Bull is { } b && bulls.ContainsKey(b)).ToList();
        var lengths = held.Select(s => Distance(centres[s.Bull!.Value], Plane(s.Image))).ToList();
        double median = Median(lengths);
        var lines = new List<BullLine>(held.Count);
        for (int i = 0; i < held.Count; i++)
        {
            var shot = held[i];
            int bull = shot.Bull!.Value;
            var doubt = LineDoubt.None;
            bool settled = shot.BullChosen || state.Dismissed?.Contains($"contested:{shot.Id}") == true;
            if (!settled)
            {
                if (state.Assignment?.For(shot.Id) is { Ambiguous: true })
                {
                    doubt |= LineDoubt.MatcherUnsure;
                }

                double own = spacing[bull];
                if (lengths[i] > LongTimesMedian * median && (double.IsNaN(own) || lengths[i] > LongShareOfSpacing * own))
                {
                    doubt |= LineDoubt.Long;
                }

                var hole = Plane(shot.Image);
                if (state.Bulls.Any(o => o.Index != bull && !double.IsNaN(spacing[o.Index])
                    && FromSegment(centres[o.Index], centres[bull], hole) < NeighbourShare * spacing[o.Index]))
                {
                    doubt |= LineDoubt.CrossesNeighbour;
                }
            }

            lines.Add(new BullLine(shot.Id, bull, bulls[bull].Image, shot.Image, doubt));
        }

        return lines;
    }

    /// <summary>The unsure holes in the order a person steps through them: by bull as the sheet lists them, then by shot.</summary>
    public static IReadOnlyList<int> Unsure(MarkingState state)
    {
        var order = state.Bulls.Select((b, i) => (b.Index, i)).ToDictionary(p => p.Index, p => p.i);
        return [.. For(state).Where(l => l.Unsure).OrderBy(l => order.GetValueOrDefault(l.Bull)).ThenBy(l => l.Shot).Select(l => l.Shot)];
    }

    /// <summary>The unsure hole after <paramref name="after"/>, going round to the first; null when there is none.</summary>
    public static int? NextUnsure(MarkingState state, int? after)
    {
        var unsure = Unsure(state);
        if (unsure.Count == 0)
        {
            return null;
        }

        int at = after is { } id ? unsure.ToList().IndexOf(id) : -1;
        return unsure[(at + 1) % unsure.Count];
    }

    /// <summary>Why a line is unsure, in words for a tooltip or a screen reader: "long for the sheet and crosses into a neighbour bull".</summary>
    public static string Words(LineDoubt doubt)
    {
        var parts = new List<string>();
        if (doubt.HasFlag(LineDoubt.MatcherUnsure))
        {
            parts.Add("GroupLab was not sure which bull it belongs to");
        }

        if (doubt.HasFlag(LineDoubt.Long))
        {
            parts.Add("its line is long for the sheet");
        }

        if (doubt.HasFlag(LineDoubt.CrossesNeighbour))
        {
            parts.Add("its line crosses into a neighboring bull");
        }

        return string.Join(", and ", parts);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>How far a point is from the segment between two others.</summary>
    private static double FromSegment(PointD p, PointD a, PointD b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double length = (dx * dx) + (dy * dy);
        double t = length == 0 ? 0 : Math.Clamp((((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / length, 0, 1);
        return Distance(p, new PointD(a.X + (t * dx), a.Y + (t * dy)));
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1.3 and 1.4: a bull chosen on the picture, and a tap on a hole giving it to that bull or taking it
/// away. The words are here so the desktop and the phone say the same thing.
/// </summary>
public static class BullChoosing
{
    /// <summary>The banner while a bull is chosen.</summary>
    public static string Banner(string bull) =>
        $"Bull {bull} chosen. Tap a hole to give it to {Named(bull)}, or tap one of its holes to take it away.";

    /// <summary>A bull as a sentence names it: "C3" says what it is, and "1" alone does not, so it is "bull 1".</summary>
    private static string Named(string bull) => bull.Length > 0 && bull.All(char.IsDigit) ? "bull " + bull : bull;

    /// <summary>"C3 has 2 shots", the holes that are shots on the bull now.</summary>
    public static string Holds(MarkingState state, int bull)
    {
        ArgumentNullException.ThrowIfNull(state);
        string label = state.Bulls.FirstOrDefault(b => b.Index == bull)?.Label ?? bull.ToString(CultureInfo.InvariantCulture);
        int count = state.Shots.Count(s => s.IsShot && s.Bull == bull);
        string named = Named(label);
        named = char.ToUpperInvariant(named[0]) + named[1..];
        return count switch
        {
            0 => $"{named} has no shots",
            1 => $"{named} has 1 shot",
            _ => string.Create(CultureInfo.InvariantCulture, $"{named} has {count} shots"),
        };
    }

    /// <summary>"No unsure holes", "1 unsure hole", "3 unsure holes".</summary>
    public static string UnsureCount(int count) => count switch
    {
        0 => "No unsure holes",
        1 => "1 unsure hole",
        _ => string.Create(CultureInfo.InvariantCulture, $"{count} unsure holes"),
    };

    /// <summary>
    /// A hole tapped while <paramref name="bull"/> is chosen: given to it, or taken away when it was already the bull's. Returns the sentence
    /// saying what happened, for the banner or the status line.
    /// </summary>
    public static string Tap(MarkingSession session, int bull, int shot)
    {
        ArgumentNullException.ThrowIfNull(session);
        var state = session.State;
        string Label(int? index) => index is { } i && state.Bulls.FirstOrDefault(b => b.Index == i) is { } aim ? aim.Label : "no bull";
        string name = ShotLabels.For(state).FirstOrDefault(l => l.ShotId == shot)?.Name is { } n ? ShotLabels.InSentence(n) : "the hole";
        if (state.Find(shot) is not { IsShot: true } hole)
        {
            return "";
        }

        if (hole.Bull == bull)
        {
            int? now = session.TakeFromBull(shot);
            return $"Took {name} away from bull {Label(bull)}; it is on bull {Label(now)} now.";
        }

        session.AssignBulls([shot], bull);
        return $"Gave {name} to bull {Label(bull)}.";
    }
}
