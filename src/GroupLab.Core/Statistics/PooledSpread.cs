using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>
/// Question 34, proposed and built in entry 386 section 5 for planning to confirm: what the centre of a group pooled from several sheets means.
/// The headline is <see cref="WithinMeanRadius"/>, each sheet measured from its own centre and the radii pooled (option B: the load and the
/// rifle, comparable with any one sheet's mean radius); beside it <see cref="AllMeanRadius"/>, every shot from one centre (option A: what the
/// rifle and shooter did across the sheets), and <see cref="Movement"/>, how far the sheets' own centres sat from where they sit on average,
/// which is the gap between the two named as its own quantity (option C).
/// </summary>
/// <param name="Sheets">The sheets with at least two shots, the only ones a centre of their own means anything for.</param>
/// <param name="Shots">Every shot pooled.</param>
/// <param name="WithinMeanRadius">Mean radius in inches, each shot from its own sheet's centre, over the sheets with two shots or more.</param>
/// <param name="AllMeanRadius">Mean radius in inches of every shot from the centre of all of them.</param>
/// <param name="Movement">The root mean square distance in inches of those sheets' centres from the mean of them.</param>
public sealed record PooledSpread(int Sheets, int Shots, double WithinMeanRadius, double AllMeanRadius, double Movement)
{
    /// <summary>
    /// The spread of shots labelled by sheet, offsets in inches, or null where fewer than two sheets have two shots or more: one sheet's
    /// group has one centre and nothing to separate.
    /// </summary>
    public static PooledSpread? Of(IReadOnlyList<(string Sheet, PointD Offset)> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        var sheets = shots.GroupBy(s => s.Sheet, StringComparer.Ordinal).Where(g => g.Count() >= 2).ToList();
        if (sheets.Count < 2)
        {
            return null;
        }

        static PointD Centre(IEnumerable<PointD> p) => new(p.Average(q => q.X), p.Average(q => q.Y));
        static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        var all = Centre(shots.Select(s => s.Offset));
        var centres = sheets.Select(g => Centre(g.Select(s => s.Offset))).ToList();
        double within = sheets.SelectMany((g, i) => g.Select(s => Distance(s.Offset, centres[i]))).Average();
        var middle = Centre(centres);
        double movement = Math.Sqrt(centres.Average(c => Math.Pow(Distance(c, middle), 2)));
        return new PooledSpread(sheets.Count, shots.Count, within, shots.Average(s => Distance(s.Offset, all)), movement);
    }

    /// <summary>Question 34: the pooled spread of an imported table, where its rows name their sheets and two or more have two shots.</summary>
    public static PooledSpread? OfTable(Marking.CsvTable table, int xColumn, int yColumn, IReadOnlyList<PointD> offsets)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        return Marking.ShotCsv.SheetsOf(table, xColumn, yColumn) is { } sheets && sheets.Count == offsets.Count
            ? Of([.. sheets.Zip(offsets)])
            : null;
    }

    /// <summary>The three figures in one sentence, the headline first.</summary>
    public string Words() => string.Create(CultureInfo.InvariantCulture,
        $"Pooled from {Sheets} sheets: mean radius {WithinMeanRadius:0.000} in with each sheet measured from its own center, the load and rifle; {AllMeanRadius:0.000} in with all {Shots} shots measured from one center, which also holds the sheets' centers moving {Movement:0.000} in between sheets.");
}
