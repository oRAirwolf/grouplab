using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Reporting;

/// <summary>One line the results box on a shared picture can carry, its key, its words, and whether it is shown until the shooter says otherwise.</summary>
public sealed record ShareLine(string Key, string Text, bool Shown);

/// <summary>The mean radius circle on the picture, in the picture's own stored pixels: its centre and radius.</summary>
public sealed record ShareCircle(PointD Centre, double Radius);

/// <summary>
/// Share A, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 features d and g, board ShareA): what goes on a picture of the target to
/// share. The results box's lines, from the analysis's counted figures so the picture says what the result says, and the mean radius circle
/// drawn about the group's centre on the photograph itself: at each aim point that holds counted shots, the group's centre from the aim
/// carried onto the picture through the marking's own scale, and the mean radius as that scale makes it there. The phone's page and the
/// desktop's window both draw from this.
/// </summary>
public static class ShareCard
{
    /// <summary>
    /// The lines the box can carry, in order: the sheet and date, the shots, mean radius, extreme spread, the centre from aim, the distance,
    /// and the rifle and load. The first four are shown to begin with; the others wait to be tapped on.
    /// </summary>
    public static IReadOnlyList<ShareLine> Lines(MarkingState state, string title, string date, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        units = units.Aiming(state.Rifle); // entry 294 section 1: angles in the scope's unit, the rifle's where one is named
        var lines = new List<ShareLine> { new("title", string.IsNullOrWhiteSpace(date) ? title : $"{title}, {date}", true) };
        var report = GroupAnalysis.Analyse(state);
        double? distance = state.ShotDistanceInches;
        string Sized(double inches) => units.AngleText(inches, distance) is { } angle ? $"{units.Length(inches)} ({angle})" : units.Length(inches);
        if (report.Counted is { } counted)
        {
            lines.Add(new("shots", report.Excluded > 0
                ? string.Create(CultureInfo.InvariantCulture, $"{counted.Shots} shots, {report.Excluded} left out")
                : string.Create(CultureInfo.InvariantCulture, $"{counted.Shots} shots"), true));
            if (counted.MeanRadius is { } mr)
            {
                lines.Add(new("mean-radius", "Mean radius " + Sized(mr.Value), true));
            }

            if (counted.ExtremeSpread is { } es)
            {
                lines.Add(new("extreme-spread", "Extreme spread " + Sized(es.Value), true));
            }

            if (counted.CentreFromAim is { } c)
            {
                lines.Add(new("center", $"Center {units.Length(Math.Abs(c.X))} {(c.X >= 0 ? "right" : "left")}, {units.Length(Math.Abs(c.Y))} {(c.Y >= 0 ? "low" : "high")}", false));
            }
        }

        if (distance is { } d)
        {
            lines.Add(new("distance", "At " + units.DistanceText(d), false));
        }

        string equipment = string.Join(", ", new[] { state.Rifle?.Name, state.Load, state.Calibre?.Name }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (equipment.Length > 0)
        {
            lines.Add(new("equipment", equipment, false));
        }

        return lines;
    }

    /// <summary>The shots that count: shots, not left out, not on a sighter; the same rule as every figure.</summary>
    private static List<MarkedShot> Counted(MarkingState state) =>
        [.. state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s))];

    /// <summary>
    /// The mean radius circle, one at each aim point holding counted shots (one for a plain group), about the group's centre, in stored
    /// pixels; none without a scale or below the shots a mean radius needs.
    /// </summary>
    public static IReadOnlyList<ShareCircle> Circles(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var counted = Counted(state);
        if (state.Scale is null || counted.Count == 0 || GroupAnalysis.Analyse(state).Counted is not { MeanRadius: { } mr })
        {
            return [];
        }

        var offsets = GroupAnalysis.CompositeOffsets(state, counted);
        if (offsets.Count != counted.Count)
        {
            return [];
        }

        var centre = GroupStatistics.Centre(offsets);
        var circles = new List<ShareCircle>();
        foreach (var group in counted.GroupBy(s => s.Bull))
        {
            // Start from the middle of this aim point's own holes on the picture and step to the point whose offset is the group's centre,
            // through the scale's local slope there: the same offset every figure is measured in, carried back onto the picture.
            var shots = group.ToList();
            var start = new PointD(shots.Average(s => s.Image.X), shots.Average(s => s.Image.Y));
            if (Local(state, group.Key, start) is not var (at, inverse, pixelsPerInch))
            {
                continue;
            }

            var gap = new PointD(centre.X - at.X, centre.Y - at.Y);
            var image = new PointD(start.X + (inverse.A * gap.X) + (inverse.B * gap.Y), start.Y + (inverse.C * gap.X) + (inverse.D * gap.Y));
            circles.Add(new ShareCircle(image, mr.Value * pixelsPerInch));
        }

        return circles;
    }

    /// <summary>
    /// The offset a shot at <paramref name="image"/> on aim point <paramref name="bull"/> would have, the inverse of the scale's slope there,
    /// and the pixels an inch makes there; null where the scale is singular.
    /// </summary>
    private static (PointD At, (double A, double B, double C, double D) Inverse, double PixelsPerInch)? Local(MarkingState state, int? bull, PointD image)
    {
        PointD Offset(PointD p) => GroupAnalysis.CompositeOffsets(state, [new MarkedShot(-1, p, ShotProvenance.Manual, Bull: bull)])[0];
        var at = Offset(image);
        const double step = 1;
        var dx = Offset(new PointD(image.X + step, image.Y));
        var dy = Offset(new PointD(image.X, image.Y + step));
        double a = (dx.X - at.X) / step, c = (dx.Y - at.Y) / step, b = (dy.X - at.X) / step, d = (dy.Y - at.Y) / step;
        double det = (a * d) - (b * c);
        if (Math.Abs(det) < 1e-18 || !double.IsFinite(det))
        {
            return null;
        }

        return (at, (d / det, -b / det, -c / det, a / det), 1 / Math.Sqrt(Math.Abs(det)));
    }

    /// <summary>
    /// The crop "Around the group": a square in stored pixels about the counted holes and the circles, with room around them, kept inside the
    /// picture; null where there is nothing to crop to.
    /// </summary>
    public static (double X, double Y, double Width, double Height)? GroupArea(MarkingState state, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(state);
        var points = Counted(state).Select(s => (s.Image, Radius: 0.0)).Concat(Circles(state).Select(c => (Image: c.Centre, c.Radius))).ToList();
        if (points.Count == 0 || width <= 0 || height <= 0)
        {
            return null;
        }

        double left = points.Min(p => p.Image.X - p.Radius), right = points.Max(p => p.Image.X + p.Radius);
        double top = points.Min(p => p.Image.Y - p.Radius), bottom = points.Max(p => p.Image.Y + p.Radius);
        double side = Math.Min(Math.Min(width, height), Math.Max(Math.Max(right - left, bottom - top) * 1.8, Math.Min(width, height) / 4));
        double x = Math.Clamp(((left + right) / 2) - (side / 2), 0, width - side);
        double y = Math.Clamp(((top + bottom) / 2) - (side / 2), 0, height - side);
        return (x, y, side, side);
    }
}
