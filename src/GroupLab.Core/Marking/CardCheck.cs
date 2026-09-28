using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>A card measured on the check page: its size in the sheet's own units, and the scale that makes it its true size.</summary>
/// <param name="WidthDmm">The card's width across the page, in the page's own dmm, before the thickness correction.</param>
/// <param name="HeightDmm">Its height down the page.</param>
/// <param name="Across">The print scale across the page the card says.</param>
/// <param name="Down">The same, down the page.</param>
/// <param name="Lift">The factor the card's face was enlarged by for being nearer the camera, 1 on a scan.</param>
/// <param name="EdgeRmsDmm">How far the found edge points lie from the straight sides fitted through them.</param>
public sealed record CardMeasure(double WidthDmm, double HeightDmm, double Across, double Down, double Lift, double EdgeRmsDmm);

/// <summary>
/// The card photo of NOTES-FROM-PLANNING.md entries 272 and 273: a bank, gift or ID card, ISO/IEC 7810 ID-1, 85.60 by 53.98 mm, laid on the
/// check page's outline and photographed. The page's markers register it, so every point on the page is known in the page's own units; the
/// card's four sides are found along lines crossing them and fitted straight, which gives its width and height in the page's units, and
/// its true size over those is the print scale across and down.
/// <para>
/// <b>Finding the edges.</b> Along each line across a side, from outside the outline inward, the photograph is compared with what the page
/// prints there: white paper, then the outline's black line. The card starts where the photograph stops looking like the page, which holds
/// for a card of any color that covers the line or lies inside it, and for a card on the line. The corners are left out, because a card's
/// are rounded (3.18 mm). A white card on white paper shows only its thin shadow, which the same test finds less surely; a card with color
/// is better, and the wizard says so.
/// </para>
/// <para>
/// <b>The card's thickness.</b> Its face is <see cref="GridStyle4.CardThicknessMm"/> above the page, nearer the camera, so it looks larger by
/// the camera's distance over that distance less the thickness: about 0.15 percent from half a meter. The distance comes from the scale
/// the markers give and the lens's focal length where the photograph states it, and half a meter where it does not.
/// </para>
/// </summary>
public static class CardCheck
{
    /// <summary>The distance assumed when a photograph states no focal length: a phone held over a Letter sheet.</summary>
    public const double AssumedDistanceMm = 500;

    private const double Step = 0.5;

    /// <summary>The card measured on a registered photograph or scan of the check page, or null where its sides could not be found.</summary>
    /// <param name="image">The photograph, in grey.</param>
    /// <param name="mapping">The page's registration, image pixels to page dmm.</param>
    /// <param name="definition">The check page.</param>
    /// <param name="pixelsPerMm">The picture's scale on the page, pixels to a millimeter, from the registration.</param>
    /// <param name="focalPixels">The lens's focal length in pixels, or null where the photograph does not say; 0 for a scan, which has no perspective.</param>
    public static CardMeasure? Measure(GrayImage image, IPageMapping mapping, TargetDefinition definition, double pixelsPerMm, double? focalPixels)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(definition);
        var card = GridStyle4.Card(definition.Page);
        double x0 = card.X, y0 = card.Y, x1 = card.X + GridStyle4.CardWidthDmm, y1 = card.Y + GridStyle4.CardHeightDmm;
        double white = PaperWhite(image, mapping, x0, y0, x1, y1);
        if (white <= 0)
        {
            return null;
        }

        // Each side: points along it, away from the rounded corners, and the direction outward.
        var left = Side(image, mapping, white, t => (x0, y0 + (t * (y1 - y0))), (-1, 0), x0);
        var right = Side(image, mapping, white, t => (x1, y0 + (t * (y1 - y0))), (1, 0), x1);
        var top = Side(image, mapping, white, t => (x0 + (t * (x1 - x0)), y0), (0, -1), y0);
        var bottom = Side(image, mapping, white, t => (x0 + (t * (x1 - x0)), y1), (0, 1), y1);
        if (left is null || right is null || top is null || bottom is null)
        {
            return null;
        }

        // Straight sides: x as a line in y for the left and right, y as a line in x for the top and bottom; the card's width is the distance
        // between its left and right sides at its middle height, its height the distance between top and bottom at its middle.
        var (la, lb, lr) = Fit(left);
        var (ra, rb, rr) = Fit(right);
        var (ta, tb, tr) = Fit(top);
        var (ba, bb, br) = Fit(bottom);
        double midY = (y0 + y1) / 2, midX = (x0 + x1) / 2;
        double width = ((ra + (rb * midY)) - (la + (lb * midY))) * Math.Cos(Math.Atan((lb + rb) / 2));
        double height = ((ba + (bb * midX)) - (ta + (tb * midX))) * Math.Cos(Math.Atan((tb + bb) / 2));
        double rms = Math.Sqrt(((lr * lr) + (rr * rr) + (tr * tr) + (br * br)) / 4);

        double lift = 1;
        if (focalPixels is not 0 && pixelsPerMm > 0)
        {
            double distance = focalPixels is { } f ? f / pixelsPerMm : AssumedDistanceMm;
            lift = distance / (distance - GridStyle4.CardThicknessMm);
        }

        double across = lift * GridStyle4.CardWidthMm * 10 / width, down = lift * GridStyle4.CardHeightMm * 10 / height;
        return across is < SheetReference.LowestBelievable or > SheetReference.HighestBelievable
            || down is < SheetReference.LowestBelievable or > SheetReference.HighestBelievable
            ? null
            : new CardMeasure(width, height, across, down, lift, rms);
    }

    /// <summary>The paper's white, from a ring of page just outside the outline, where nothing is printed.</summary>
    private static double PaperWhite(GrayImage image, IPageMapping mapping, double x0, double y0, double x1, double y1)
    {
        var samples = new List<double>();
        for (double t = 0.1; t <= 0.9; t += 0.05)
        {
            foreach (var (x, y) in new[] { (x0 - 60, y0 + (t * (y1 - y0))), (x1 + 60, y0 + (t * (y1 - y0))), (x0 + (t * (x1 - x0)), y0 - 60), (x0 + (t * (x1 - x0)), y1 + 60) })
            {
                if (Sample(image, mapping, x, y) is { } v)
                {
                    samples.Add(v);
                }
            }
        }

        return samples.Count < 8 ? 0 : samples.Order().ElementAt(samples.Count * 3 / 4);
    }

    /// <summary>
    /// One side's edge points, in page dmm along the side's own axis (x for the left and right, y for the top and bottom), or null where too
    /// few were found. Walking inward from well outside the outline, the page should show white, the outline's black, then white again up to
    /// the card; the card starts where the photograph stays unlike that for 3 dmm, and its edge is where the grey crosses halfway between the
    /// paper and the card. A white card's edge is its shadow, the darkest line there.
    /// </summary>
    private static List<(double Along, double Edge)>? Side(GrayImage image, IPageMapping mapping, double white, Func<double, (double X, double Y)> at, (int X, int Y) outward, double nominal)
    {
        const int Lines = 40;
        const double Band = 2.5, Unlike = 0.25;
        double lineIn = GridStyle4.OutlineGap, lineOut = GridStyle4.OutlineGap + GridStyle4.OutlineStroke;
        var found = new List<(double, double)>();
        for (int k = 0; k < Lines; k++)
        {
            var (px, py) = at(0.15 + (0.7 * k / (Lines - 1)));
            var s = new List<double>();
            var v = new List<double>();
            for (double u = lineOut + 30; u >= -60; u -= Step)
            {
                if (Sample(image, mapping, px + (outward.X * u), py + (outward.Y * u)) is not { } g)
                {
                    break;
                }

                s.Add(u);
                v.Add(g);
            }

            double ink = Sample(image, mapping, px + (outward.X * (lineIn + lineOut) / 2), py + (outward.Y * (lineIn + lineOut) / 2)) ?? 0;
            bool Blurred(double u) => Math.Abs(u - lineIn) < Band || Math.Abs(u - lineOut) < Band;
            bool Off(int n) => !Blurred(s[n]) && Math.Abs(v[n] - (s[n] > lineIn && s[n] <= lineOut ? ink : white)) / white > Unlike;
            int run = (int)(3 / Step), start = -1;
            for (int n = 0; n + run < s.Count; n++)
            {
                if (s[n] < lineIn - Band && Enumerable.Range(n, run).All(Off))
                {
                    start = n;
                    break;
                }
            }

            if (start < 0)
            {
                continue;
            }

            // The card's own level, a little inside the edge; the edge where the grey crosses halfway from the paper to it.
            var inside = Enumerable.Range(start + (int)(3 / Step), (int)(5 / Step)).Where(n => n < v.Count).Select(n => v[n]).Order().ToList();
            if (inside.Count == 0)
            {
                continue;
            }

            double card = inside[inside.Count / 2];
            double? edge = null;
            int from = Math.Max(0, start - (int)(3 / Step)), to = Math.Min(v.Count - 2, start + (int)(3 / Step));
            if (Math.Abs(white - card) / white >= 0.15)
            {
                double half = (white + card) / 2;
                for (int n = from; n <= to && edge is null; n++)
                {
                    if (s[n] < lineIn - Band && (v[n] - half) * (v[n + 1] - half) <= 0 && v[n] != v[n + 1])
                    {
                        edge = s[n] - (Step * (v[n] - half) / (v[n] - v[n + 1]));
                    }
                }
            }
            else
            {
                int darkest = Enumerable.Range(from + 1, Math.Max(0, to - from - 1)).Where(n => s[n] < lineIn - Band).DefaultIfEmpty(-1).MinBy(n => n < 0 ? double.MaxValue : v[n]);
                if (darkest > 0 && darkest + 1 < v.Count)
                {
                    double a = v[darkest - 1], b = v[darkest], c = v[darkest + 1], d = a - (2 * b) + c;
                    edge = s[darkest] - (d > 0 ? Step * (a - c) / (2 * d) : 0);
                }
            }

            if (edge is { } e)
            {
                found.Add((outward.X != 0 ? py : px, outward.X != 0 ? px + (outward.X * e) : py + (outward.Y * e)));
            }
        }

        if (found.Count < Lines / 2)
        {
            return null;
        }

        // A point far from the median of its side is a speck, a finger or glare, not the card's edge.
        double median = found.Select(f => f.Item2 - nominal).Order().ElementAt(found.Count / 2);
        var kept = found.Where(f => Math.Abs(f.Item2 - nominal - median) <= 6).ToList();
        return kept.Count < Lines / 3 ? null : kept;
    }

    /// <summary>A least squares line, the edge as a + b times along, and the points' root mean square distance from it.</summary>
    private static (double A, double B, double Rms) Fit(List<(double Along, double Edge)> points)
    {
        double n = points.Count, sx = points.Sum(p => p.Along), sy = points.Sum(p => p.Edge);
        double sxx = points.Sum(p => p.Along * p.Along), sxy = points.Sum(p => p.Along * p.Edge);
        double b = ((n * sxy) - (sx * sy)) / ((n * sxx) - (sx * sx));
        double a = (sy - (b * sx)) / n;
        double rms = Math.Sqrt(points.Sum(p => Math.Pow(p.Edge - (a + (b * p.Along)), 2)) / n);
        return (a, b, rms);
    }

    /// <summary>The photograph's grey at a page point, by bilinear interpolation, or null off the picture.</summary>
    private static double? Sample(GrayImage image, IPageMapping mapping, double x, double y)
    {
        var p = mapping.ToImage(new PointD(x, y));
        int ix = (int)Math.Floor(p.X), iy = (int)Math.Floor(p.Y);
        if (ix < 0 || iy < 0 || ix + 1 >= image.Width || iy + 1 >= image.Height)
        {
            return null;
        }

        double fx = p.X - ix, fy = p.Y - iy;
        byte[] px = image.Pixels;
        int w = image.Width, i = (iy * w) + ix;
        return (px[i] * (1 - fx) * (1 - fy)) + (px[i + 1] * fx * (1 - fy)) + (px[i + w] * (1 - fx) * fy) + (px[i + w + 1] * fx * fy);
    }
}
