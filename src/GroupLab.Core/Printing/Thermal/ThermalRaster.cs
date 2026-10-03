using System.Globalization;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Printing.Thermal;

/// <summary>Where a page narrower than the head sits on it, and which part of a wider one is kept.</summary>
public enum HeadAlignment
{
    /// <summary>The page's middle on the head's middle, as a label roll centred by its guides feeds.</summary>
    Centre,

    /// <summary>The page's left edge on the head's first dot, as a roll held against one side feeds.</summary>
    Left,
}

/// <summary>
/// A thermal printer's head, NOTES-FROM-PLANNING.md entry 358 section 2: its dots per inch (203.2 for 8 dots a millimetre, 300 or 304.8 for the
/// two kinds sold as "300 dpi"), how many dots it has across, and where the paper sits on it.
/// </summary>
public sealed record PrintHead(double DotsPerInch, int Dots, HeadAlignment Alignment = HeadAlignment.Centre)
{
    /// <summary>Dots per half-dmm, the scene's unit.</summary>
    public double DotsPerUnit => DotsPerInch / PrintFit.UnitsPerInch;

    /// <summary>The head's width in millimetres.</summary>
    public double WidthMm => Dots * 25.4 / DotsPerInch;
}

/// <summary>One page as the head will print it: the dots, how wide the page is in dots, and the words for anything that will not print.</summary>
/// <param name="CutLeft">Dots of the page's left side the head does not reach; the image starts there.</param>
/// <param name="CutOff">What would be cut off, said before printing, or null where the whole page prints.</param>
public sealed record ThermalPage(DotImage Image, int PageDots, int CutLeft, string? CutOff);

/// <summary>
/// The thermal print mode of NOTES-FROM-PLANNING.md entry 358 section 2, used for every thermal printer whatever the page size: the page drawn
/// one bit deep at the printer's own dot pitch, black or paper, no grey, no smoothing and no dithering of the artwork, and never fitted to the
/// page.
/// <list type="bullet">
/// <item><b>Every edge on a whole dot.</b> A marker or code is drawn with every module the same whole number of dots, centred on where the
/// definition puts it, so its centre is never more than half a dot from true. A QR code placed between dots prints squares 3 and 4 dots wide
/// at random (measured on a simulated 203 dpi print); snapped, it prints clean. A tag's 0.5 mm module is exactly 4 dots at 8 dots a
/// millimetre; a code's 0.4 mm module becomes 3 dots, 0.375 mm, at 203 dpi and 5 at 300.</item>
/// <item><b>Rules and lines keep their width.</b> A rectangle that is not a module is rounded to whole dots with its width rounded once, so a
/// line does not print one dot wider or narrower depending on where it falls.</item>
/// <item><b>Bulls and words</b> take every dot whose middle is inside them, which is a coverage of one half.</item>
/// <item><b>Black only.</b> A bull's colour is ink like any other: a thermal printer has one.</item>
/// </list>
/// </summary>
public static class ThermalRaster
{
    /// <summary>The page as the head prints it.</summary>
    public static ThermalPage Render(Scene page, PrintHead head)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(head);
        double s = head.DotsPerUnit;
        int pageDots = (int)Math.Round(page.Width * s), heightDots = (int)Math.Round(page.Height * s);
        int width = Math.Min(pageDots, head.Dots);
        int cutLeft = pageDots <= head.Dots ? 0 : head.Alignment == HeadAlignment.Centre ? (pageDots - head.Dots) / 2 : 0;
        var image = new DotImage(width, heightDots);

        var smooth = new List<SceneItem>();
        foreach (var item in page.Items)
        {
            switch (item)
            {
                case RectFill rect:
                    var (x0, y0, x1, y1) = Dots(rect, s);
                    image.Fill(x0 - cutLeft, y0, x1 - cutLeft, y1);
                    break;
                case DiscBand or TextRun:
                    smooth.Add(item);
                    break;
            }
        }

        if (smooth.Count > 0)
        {
            // Every ink is black here, so the grey render's darkness is coverage, and a dot is black where at least half of it is covered.
            var grey = SceneRasterizer.Rasterize(page with { Items = smooth }, head.DotsPerInch, region: new PixelRegion(cutLeft, 0, width, heightDots),
                words: true, level: _ => 0);
            for (int y = 0; y < heightDots; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grey.Pixels[(y * width) + x] < 128)
                    {
                        image[x, y] = true;
                    }
                }
            }
        }

        return new ThermalPage(image, pageDots, cutLeft, CutOff(page, head));
    }

    /// <summary>
    /// A rectangle in dots, from (x0, y0) up to (x1, y1). A module run is placed on its symbol's own grid of whole dots: the module is the
    /// nearest whole number of dots, at least one, and the symbol is centred where the definition centres it.
    /// </summary>
    public static (long X0, long Y0, long X1, long Y1) Dots(RectFill rect, double dotsPerUnit)
    {
        ArgumentNullException.ThrowIfNull(rect);
        if (rect.Module is { } m)
        {
            long k = ModuleDots(m.Size, dotsPerUnit);
            double symbol = m.Size * (double)m.Modules;
            double left = rect.X - (m.Column * (double)m.Size), top = rect.Y - (m.Row * (double)m.Size);
            long originX = (long)Math.Round(((left + (symbol / 2)) * dotsPerUnit) - (m.Modules * k / 2.0), MidpointRounding.AwayFromZero);
            long originY = (long)Math.Round(((top + (symbol / 2)) * dotsPerUnit) - (m.Modules * k / 2.0), MidpointRounding.AwayFromZero);
            long columns = rect.Width / m.Size, rows = rect.Height / m.Size;
            return (originX + (m.Column * k), originY + (m.Row * k), originX + ((m.Column + columns) * k), originY + ((m.Row + rows) * k));
        }

        var (left0, width) = Span(rect.X, rect.Width, dotsPerUnit);
        var (top0, height) = Span(rect.Y, rect.Height, dotsPerUnit);
        return (left0, top0, left0 + width, top0 + height);
    }

    /// <summary>How many dots one module of <paramref name="moduleUnits"/> half-dmm prints as: the nearest whole number, never none.</summary>
    public static long ModuleDots(long moduleUnits, double dotsPerUnit) => Math.Max(1, (long)Math.Round(moduleUnits * dotsPerUnit, MidpointRounding.AwayFromZero));

    private static (long Start, long Length) Span(long from, long length, double s)
    {
        long dots = Math.Max(1, (long)Math.Round(length * s, MidpointRounding.AwayFromZero));
        long start = (long)Math.Round(((from + (length / 2.0)) * s) - (dots / 2.0), MidpointRounding.AwayFromZero);
        return (start, dots);
    }

    /// <summary>
    /// What would be cut off when the page is wider than the head, said before printing, or null where everything prints. GroupLab never
    /// shrinks a page to fit (entry 358 section 2), so the words say which edges are lost and what is drawn there.
    /// </summary>
    public static string? CutOff(Scene page, PrintHead head)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(head);
        double s = head.DotsPerUnit;
        int pageDots = (int)Math.Round(page.Width * s);
        if (pageDots <= head.Dots)
        {
            return null;
        }

        int cutLeft = head.Alignment == HeadAlignment.Centre ? (pageDots - head.Dots) / 2 : 0;
        double left = cutLeft / s, right = (cutLeft + head.Dots) / s;
        var lost = page.Items.Where(i => i is not ImageBox).Select(i => (What: PrintFit.What(i), Extent: PrintFit.Extent(i)))
            .Where(x => x.Extent.Left < left || x.Extent.Right > right).Select(x => x.What).Distinct().ToList();
        var inv = CultureInfo.InvariantCulture;
        string edges = cutLeft > 0
            ? string.Create(inv, $"{left / 20:0.0} mm at the left and {(page.Width - right) / 20:0.0} mm at the right")
            : string.Create(inv, $"{(page.Width - right) / 20:0.0} mm at the right");
        string what = lost.Count == 0 ? "only blank paper is there" : $"the {string.Join(" and ", lost)} there would be missing";
        return string.Create(inv, $"This page is {page.Width / 20.0:0.0} mm wide and the printer's head prints {head.WidthMm:0.0} mm, so {edges} would be cut off, and {what}. ")
            + "GroupLab never shrinks a target to fit, because a shrunk target measures every group wrong. Choose a size this printer takes.";
    }
}
