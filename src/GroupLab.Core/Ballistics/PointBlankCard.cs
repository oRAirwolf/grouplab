using System.Globalization;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Ballistics;

/// <summary>
/// The point blank card, NOTES-FROM-PLANNING.md entry 455: a 50 by 30 mm label for the rifle (the M220's label, and the same page on any
/// other label or sheet printer, printed at 100 percent). Six lines: the rifle; the bullet, velocity and BC; the dead-on distances, large; the
/// zone and the certainty; the zero at the rifle's zero distance with its clicks; and the date it was worked out. Nothing on it is round, so
/// stuck on a target it cannot be taken for a hole.
/// </summary>
public static class PointBlankCard
{
    public const int WidthMm = 50, HeightMm = 30;

    /// <summary>Scene units to the millimetre, as the scale labels use.</summary>
    private const double U = 20;

    private static readonly Gltd.Binary.Rgb Black = new(0, 0, 0);

    /// <summary>The card's lines, top to bottom, which the page draws and the tests read.</summary>
    public static IReadOnlyList<string> Lines(Rifle rifle, Load load, PointBlankResult result, PointBlankRequest request, UnitSettings units, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(units);
        string bullet = string.Join(" ", new[]
        {
            load.BulletWeightGrains is { } w ? w.ToString("0.#", CultureInfo.InvariantCulture) + " gr" : null,
            load.BulletName,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        string flight = string.Join(" · ", new[]
        {
            bullet.Length > 0 ? bullet : load.Name,
            load.MuzzleVelocityFps is { } v ? units.Speed(v) : null,
            load.BallisticCoefficient is { } bc ? $"{load.DragModel} {bc.ToString("0.000", CultureInfo.InvariantCulture)}" : null,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // The setting line without its "from your ... zero" tail, which the card has no room for and the line already says by its distance.
        string setting = result.HowToSet.Replace(" from your ", "|", StringComparison.Ordinal).Split('|')[0].TrimEnd('.');
        return
        [
            rifle.Name,
            flight,
            result.Headline.ToUpperInvariant(),
            $"{request.ZoneName}, {PointBlankReport.Share(request.Certainty)} shots",
            setting,
            "GroupLab point blank · " + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ];
    }

    /// <summary>The card as a page exactly the label's size.</summary>
    public static Scene Page(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var items = new List<SceneItem>();
        double y = 4.2;
        for (int i = 0; i < lines.Count; i++)
        {
            bool big = i == 2;
            double size = big ? 3.6 : i == lines.Count - 1 ? 2.0 : 2.6;
            items.Add(new TextRun(SceneLayer.Labels, Black, (long)Math.Round(2 * U), (long)Math.Round(y * U), (long)Math.Round(size * U), Fit(lines[i], size), TextAnchor.Left, Bold: i is 0 or 2));
            y += big ? 5.4 : 4.4;
        }

        return new Scene((long)Math.Round(WidthMm * U), (long)Math.Round(HeightMm * U), 0, items);
    }

    /// <summary>A line shortened to what fits 46 mm at its size, with an ellipsis, so nothing runs off the label's edge.</summary>
    internal static string Fit(string line, double sizeMm)
    {
        int most = (int)Math.Floor(46 / (sizeMm * 0.56));
        return line.Length <= most ? line : line[..(most - 1)].TrimEnd() + "…";
    }

    /// <summary>The file name the card is saved or shared under.</summary>
    public static string FileName(Rifle rifle)
    {
        ArgumentNullException.ThrowIfNull(rifle);
        string safe = new([.. rifle.Name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')]);
        return $"grouplab-point-blank-{safe.Trim('-')}-{WidthMm}x{HeightMm}mm";
    }
}
