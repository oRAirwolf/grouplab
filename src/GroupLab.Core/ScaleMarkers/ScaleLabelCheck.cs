using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>What a scanned printer check label measured: the scale across the head, and along the feed where the label has two rows.</summary>
/// <param name="Across">How large the printer printed across the label: 1.002 is 100.2 percent.</param>
/// <param name="Along">The same along the paper feed, or null where the label holds one row of codes.</param>
/// <param name="Codes">How many of the label's codes were read.</param>
public sealed record LabelCheck(double Across, double? Along, int Codes)
{
    /// <summary>The check in words, for the screen that measured it.</summary>
    public string Words(int width, int height) => string.Create(CultureInfo.InvariantCulture,
        $"The {width} x {height} mm check label printed {Across * 100:0.00} percent across the head{(Along is { } along ? string.Create(CultureInfo.InvariantCulture, $" and {along * 100:0.00} percent along the feed.") : "; it has one row of codes, so the feed was not measured.")}");
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 372 section 2, done in entry 386 section 3: the printer check for a label printer such as the M220. A label of
/// the size loaded, its codes in a row at the top and another at the bottom, and fine marks across and along it for a caliper. Scanned at
/// 600 dpi, the scan's own resolution is the ruler: the codes' spacing across gives the scale across the head (expected exact, since the dot
/// pitch is fixed) and the rows' distance apart gives the scale along the feed (expected not). Scale labels are read across only, so the
/// feed figure is kept as measured, never used to correct a label's reading.
/// </summary>
public static class ScaleLabelCheck
{
    /// <summary>How far the top row's centre is from the label's top edge, and the bottom row's from the line of words, millimetres.</summary>
    private const double TopRow = 5, BottomRow = 9;

    /// <summary>The rows' centres apart, millimetres, on a label of this height with two rows.</summary>
    public static double RowsApart(int height) => height - TopRow - BottomRow;

    /// <summary>The check label of this size: the rows of codes for <paramref name="serial"/>, a ruler of millimetre marks each way, and its line of words.</summary>
    public static Scene Page(int width, int height, int serial, string printer)
    {
        double u = ScaleLabels.U;
        var items = new List<SceneItem>();
        var pairs = ScaleLabels.Pairs(width, height, serial);
        double[] rows = pairs.Count == 2 ? [TopRow, height - BottomRow] : [TopRow];
        for (int r = 0; r < pairs.Count; r++)
        {
            ScaleLabels.DrawCode(items, pairs[r].Left, ScaleLabels.Inset, rows[r]);
            ScaleLabels.DrawCode(items, pairs[r].Right, width - ScaleLabels.Inset, rows[r]);
        }

        // Millimetre marks: across, between the two rows' codes; along, down the middle. Every fifth mark longer. Thin lines only, nothing round.
        long line = (long)Math.Round(0.15 * u);
        double across = pairs.Count == 2 ? (rows[0] + rows[1]) / 2 : rows[0] + 7;
        for (int mm = ScaleLabels.Inset; mm <= width - ScaleLabels.Inset; mm++)
        {
            double tall = mm % 5 == 0 ? 2.5 : 1.2;
            items.Add(new RectFill(SceneLayer.Labels, ScaleLabels.Black, (long)Math.Round(mm * u) - (line / 2), (long)Math.Round((across - tall) * u), line, (long)Math.Round(tall * u)));
        }

        if (pairs.Count == 2)
        {
            double middle = width / 2.0;
            for (int mm = (int)Math.Ceiling(rows[0] + 5); mm <= rows[1] - 5; mm++)
            {
                double wide = mm % 5 == 0 ? 2.5 : 1.2;
                items.Add(new RectFill(SceneLayer.Labels, ScaleLabels.Black, (long)Math.Round(middle * u), (long)Math.Round(mm * u) - (line / 2), (long)Math.Round(wide * u), line));
            }
        }

        double size = Math.Min(2.4, (width - 2) / 26.0);
        string words = string.Create(CultureInfo.InvariantCulture, $"GroupLab printer check {width} x {height} mm · {printer}");
        items.Add(new TextRun(SceneLayer.Labels, ScaleLabels.Black, (long)Math.Round(width / 2.0 * u), (long)Math.Round((height - 1.5) * u), (long)Math.Round(size * u), words, TextAnchor.Centre));
        return new Scene((long)Math.Round(width * u), (long)Math.Round(height * u), 0, items);
    }

    /// <summary>
    /// The scales a scan of the check label gives, from the markers found in it and the scan's resolution, or null where no row of a label
    /// <paramref name="width"/> mm wide was read whole. Each row read whole gives the scale across; two give the distance between them,
    /// measured square to the rows, so a label laid crooked on the glass measures the same.
    /// </summary>
    public static LabelCheck? Measure(IReadOnlyList<DetectedMarker> markers, double dotsPerInch, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(markers);
        if (!(dotsPerInch > 0))
        {
            return null;
        }

        double perMm = dotsPerInch / 25.4;
        var rows = markers
            .Where(m => ScaleLabels.Tag(m.Id) is { } tag && ScaleLabels.Widths[(m.Id - ScaleLabels.First) / ScaleLabels.Block] == width)
            .GroupBy(m => ScaleLabels.Tag(m.Id)!.Piece)
            .Select(g => (Left: g.FirstOrDefault(m => ScaleLabels.Tag(m.Id)!.Centre.X < width / 2.0), Right: g.FirstOrDefault(m => ScaleLabels.Tag(m.Id)!.Centre.X > width / 2.0)))
            .Where(r => r.Left is not null && r.Right is not null)
            .Select(r => (A: Centre(r.Left!), B: Centre(r.Right!)))
            .ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        double designed = width - (2 * ScaleLabels.Inset);
        double acrossScale = rows.Average(r => Distance(r.A, r.B) / perMm / designed);
        double? along = null;
        if (rows.Count == 2 && ScaleLabels.Rows(height) == 2)
        {
            // Square to the rows: the mean direction across, and the second row's middle measured from the first's along its normal.
            double dx = (rows[0].B.X - rows[0].A.X) + (rows[1].B.X - rows[1].A.X), dy = (rows[0].B.Y - rows[0].A.Y) + (rows[1].B.Y - rows[1].A.Y);
            double n = Math.Sqrt((dx * dx) + (dy * dy));
            var m0 = Mid(rows[0].A, rows[0].B);
            var m1 = Mid(rows[1].A, rows[1].B);
            double apart = Math.Abs(((m1.X - m0.X) * -dy / n) + ((m1.Y - m0.Y) * dx / n));
            along = apart / perMm / RowsApart(height);
        }

        return new LabelCheck(acrossScale, along, rows.Count * 2);
    }

    private static PointD Centre(DetectedMarker m) => new(m.Corners.Average(c => c.X), m.Corners.Average(c => c.Y));

    private static PointD Mid(PointD a, PointD b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
