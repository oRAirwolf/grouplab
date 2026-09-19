using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Measurement;

/// <summary>
/// One photograph measured against its sheet's scan, NOTES-FROM-PLANNING.md entry 113 section 4: the registration model used, the bull-centre
/// error against the scan, worst and median, the holes found, missed and false against the scan's holes, and the hole-position error of those
/// matched, median, 95th percentile and worst. Lengths are in inches on the page.
/// </summary>
public sealed record PhotoComparisonRow(
    string Photo,
    string Model,
    int BullsCompared,
    double? BullWorstInches,
    double? BullMedianInches,
    int TruthHoles,
    int Found,
    int Missed,
    int False,
    double? HoleMedianInches,
    double? HoleP95Inches,
    double? HoleWorstInches);

/// <summary>
/// Photographs against the scan of the same sheet, which is the truth for them: the same holes, measured flat. It reports against the gates'
/// thresholds and does not decide the gate, which is planning's to read from the table.
/// </summary>
public static class PhotoComparison
{
    /// <summary>The bull-centre threshold the table is read against, in inches.</summary>
    public const double BullThresholdInches = 0.005;

    /// <summary>How near a photograph's hole must be to a scan's hole to be the same hole, in inches: the hole-matching threshold.</summary>
    public const double HoleMatchInches = 0.15;

    private const double DmmPerInch = 254;

    /// <summary>
    /// Compares one photograph's bulls and holes with the scan's, every position in page dmm. Holes are paired nearest first, each at most
    /// once and only within <see cref="HoleMatchInches"/>; a scan hole left unpaired is missed and a photograph hole left unpaired is false.
    /// </summary>
    public static PhotoComparisonRow Compare(
        string photo,
        string model,
        IReadOnlyDictionary<int, PointD> truthBulls,
        IReadOnlyList<PointD> truthHoles,
        IReadOnlyDictionary<int, PointD> photoBulls,
        IReadOnlyList<PointD> photoHoles)
    {
        ArgumentNullException.ThrowIfNull(truthBulls);
        ArgumentNullException.ThrowIfNull(truthHoles);
        ArgumentNullException.ThrowIfNull(photoBulls);
        ArgumentNullException.ThrowIfNull(photoHoles);
        static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)) / DmmPerInch;

        var bullErrors = truthBulls.Where(t => photoBulls.ContainsKey(t.Key)).Select(t => Distance(t.Value, photoBulls[t.Key])).Order().ToList();

        var pairs = new List<(int Truth, int Photo, double Distance)>();
        for (int t = 0; t < truthHoles.Count; t++)
        {
            for (int p = 0; p < photoHoles.Count; p++)
            {
                double d = Distance(truthHoles[t], photoHoles[p]);
                if (d <= HoleMatchInches)
                {
                    pairs.Add((t, p, d));
                }
            }
        }

        var usedTruth = new HashSet<int>();
        var usedPhoto = new HashSet<int>();
        var matched = new List<double>();
        foreach (var (t, p, d) in pairs.OrderBy(x => x.Distance))
        {
            if (usedTruth.Add(t))
            {
                if (usedPhoto.Add(p))
                {
                    matched.Add(d);
                }
                else
                {
                    usedTruth.Remove(t);
                }
            }
        }

        matched.Sort();
        return new PhotoComparisonRow(
            photo,
            model,
            bullErrors.Count,
            bullErrors.Count > 0 ? bullErrors[^1] : null,
            Median(bullErrors),
            truthHoles.Count,
            photoHoles.Count,
            truthHoles.Count - matched.Count,
            photoHoles.Count - matched.Count,
            Median(matched),
            matched.Count > 0 ? matched[Math.Clamp((int)Math.Ceiling(0.95 * matched.Count) - 1, 0, matched.Count - 1)] : null,
            matched.Count > 0 ? matched[^1] : null);
    }

    private static double? Median(List<double> sorted) => sorted.Count == 0 ? null
        : sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2;

    /// <summary>The table, one row per photograph, read against the two thresholds and deciding nothing.</summary>
    public static IReadOnlyList<string> Table(IReadOnlyList<PhotoComparisonRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var inv = CultureInfo.InvariantCulture;
        static string In(double? value) => value is { } v ? v.ToString("0.0000", CultureInfo.InvariantCulture) : "-";
        var lines = new List<string>
        {
            "photograph                     model       bulls  bull worst  bull median  truth  found  missed  false  hole median  hole p95  hole worst",
        };
        foreach (var r in rows)
        {
            string name = r.Photo.Length <= 30 ? r.Photo : r.Photo[..30];
            lines.Add(string.Create(inv,
                $"{name,-30} {r.Model,-11} {r.BullsCompared,5} {In(r.BullWorstInches),11} {In(r.BullMedianInches),12} {r.TruthHoles,6} {r.Found,6} {r.Missed,7} {r.False,6} {In(r.HoleMedianInches),12} {In(r.HoleP95Inches),9} {In(r.HoleWorstInches),11}"));
        }

        lines.Add(string.Create(inv,
            $"Lengths in inches on the page. Read against {BullThresholdInches:0.000} in for bull centres and {HoleMatchInches:0.00} in for hole matching, the gates' thresholds; this table decides neither gate."));
        return lines;
    }
}
