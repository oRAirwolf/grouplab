using System.Globalization;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Printing;

/// <summary>
/// A printer's page as its driver reports it, in device pixels, the numbers GDI's <c>GetDeviceCaps</c> returns: resolution, the whole paper
/// (<c>PHYSICALWIDTH</c>, <c>PHYSICALHEIGHT</c>), where the printable area starts on it (<c>PHYSICALOFFSETX</c>, <c>PHYSICALOFFSETY</c>) and
/// how large that area is (<c>HORZRES</c>, <c>VERTRES</c>). Described as plain numbers so everything decided from it is tested with no printer.
/// </summary>
public sealed record PrinterPage(
    string Printer, int DpiX, int DpiY, int PaperWidth, int PaperHeight, int OffsetX, int OffsetY, int PrintableWidth, int PrintableHeight)
{
    /// <summary>The paper in the scene's units, half-dmm.</summary>
    public double PaperWidthUnits => PaperWidth * PrintFit.UnitsPerInch / DpiX;

    public double PaperHeightUnits => PaperHeight * PrintFit.UnitsPerInch / DpiY;

    /// <summary>The printable area on the paper, in half-dmm from the paper's top-left corner.</summary>
    public double PrintableLeft => OffsetX * PrintFit.UnitsPerInch / DpiX;

    public double PrintableTop => OffsetY * PrintFit.UnitsPerInch / DpiY;

    public double PrintableRight => (OffsetX + PrintableWidth) * PrintFit.UnitsPerInch / DpiX;

    public double PrintableBottom => (OffsetY + PrintableHeight) * PrintFit.UnitsPerInch / DpiY;
}

/// <summary>
/// GDI's anisotropic mapping that draws a scene at actual size: <c>SetWindowExtEx</c> to <see cref="WindowExtX"/> by <see cref="WindowExtY"/>,
/// <c>SetViewportExtEx</c> to the viewport extents and <c>SetViewportOrgEx</c> to the origin. GDI's device origin is the corner of the printable
/// area, not of the paper, so the origin is the physical offset negated, and a scene point lands where it belongs on the paper.
/// </summary>
public sealed record GdiMapping(int WindowExtX, int WindowExtY, int ViewportExtX, int ViewportExtY, int ViewportOrgX, int ViewportOrgY)
{
    /// <summary>Where GDI puts a scene point, in device pixels, by GDI's own formula.</summary>
    public (double X, double Y) ToDevice(double x, double y) =>
        ((x * ViewportExtX / WindowExtX) + ViewportOrgX, (y * ViewportExtY / WindowExtY) + ViewportOrgY);
}

/// <summary>What became of a print from inside GroupLab.</summary>
public enum PrintOutcomeKind
{
    Sent,
    Cancelled,
    Refused,
    Failed,
}

/// <summary>What became of a print, with the words the person is shown.</summary>
public sealed record PrintOutcome(PrintOutcomeKind Kind, string Message, string? Printer = null, int Pages = 0);

/// <summary>
/// Whether a sheet can print at actual size on a printer as it is set, NOTES-FROM-PLANNING.md entry 107 section 2, as plain functions over the
/// scene and a described printer. Two refusals, each with its reason, and never a scaling:
/// <list type="bullet">
/// <item><b>The paper:</b> a paper that is not the sheet's page, such as a Letter sheet on A4, names both.</item>
/// <item><b>The margin:</b> any item drawn in ink that falls in the printer's unprintable margin, wholly or partly, names the edge, the margin
/// and what reaches into it. Every scene item is ink, since a paper ink is never an item, so this covers markers, codes, bull artwork, rules
/// and text. <b>A marker's quiet zone is not an item and is not checked:</b> the unprintable margin leaves bare paper, and the quiet zone is
/// bare paper by design, so a quiet zone in the margin prints exactly as it would have. That is Alan's decision in entry 107, over the
/// recommendation in question 21 to refuse on it.</item>
/// </list>
/// </summary>
public static class PrintFit
{
    /// <summary>Scene units per inch: half-dmm, 508 to the inch.</summary>
    public const double UnitsPerInch = 2 * 254;

    /// <summary>How far the paper may differ from the page and still be the same paper: 1 mm, as drivers round their sizes to device pixels.</summary>
    public const double PaperTolerance = 20;

    /// <summary>
    /// Slack on the margin, 0.025 mm: the printable area arrives in whole device pixels, and an item on the pixel boundary itself is inside.
    /// </summary>
    public const double MarginSlack = 0.5;

    private static readonly (string Name, double Width, double Height)[] Papers =
    [
        ("Letter", 4318, 5588),
        ("A4", 4200, 5940),
        ("Legal", 4318, 7112),
        ("Tabloid", 5588, 8636),
        ("A3", 5940, 8400),
    ];

    /// <summary>The mapping that draws a scene at actual size on this printer.</summary>
    public static GdiMapping Mapping(PrinterPage printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return new GdiMapping((int)UnitsPerInch, (int)UnitsPerInch, printer.DpiX, printer.DpiY, -printer.OffsetX, -printer.OffsetY);
    }

    /// <summary>A page size in words: a named paper when it is one, and millimetres always.</summary>
    public static string PaperName(double width, double height)
    {
        string size = string.Create(CultureInfo.InvariantCulture, $"{width / 20:0.#} x {height / 20:0.#} mm");
        foreach (var (name, w, h) in Papers)
        {
            if (Math.Abs(width - w) <= PaperTolerance && Math.Abs(height - h) <= PaperTolerance)
            {
                return $"{name} ({size})";
            }

            if (Math.Abs(width - h) <= PaperTolerance && Math.Abs(height - w) <= PaperTolerance)
            {
                return $"{name} landscape ({size})";
            }
        }

        return size + string.Create(CultureInfo.InvariantCulture, $" ({width / UnitsPerInch:0.##} x {height / UnitsPerInch:0.##} in)");
    }

    /// <summary>The paper refusal, or null when the paper is the sheet's page.</summary>
    public static string? PaperRefusal(Scene page, PrinterPage printer)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(printer);
        if (Math.Abs(printer.PaperWidthUnits - page.Width) <= PaperTolerance && Math.Abs(printer.PaperHeightUnits - page.Height) <= PaperTolerance)
        {
            return null;
        }

        return $"{printer.Printer} is set to {PaperName(printer.PaperWidthUnits, printer.PaperHeightUnits)} paper, and this sheet is "
            + $"{PaperName(page.Width, page.Height)}. Choose that paper in the printer's preferences and print again, or, if this printer does not "
            + "take it, save the PDF and print it on one that does. GroupLab never scales a sheet to fit, so nothing was printed.";
    }

    /// <summary>An item's inked extent in half-dmm: left, top, right and bottom.</summary>
    public static (double Left, double Top, double Right, double Bottom) Extent(SceneItem item) => item switch
    {
        RectFill r => (r.X, r.Y, r.X + r.Width, r.Y + r.Height),
        DiscBand d => (d.CentreX - d.OuterRadius, d.CentreY - d.OuterRadius, d.CentreX + d.OuterRadius, d.CentreY + d.OuterRadius),
        TextRun t => TextExtent(t),
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.GetType().Name, "an item with no extent"),
    };

    private static (double, double, double, double) TextExtent(TextRun t)
    {
        long width = HelveticaMetrics.TextWidth(t.Text, t.FontSize);
        double left = t.Anchor switch
        {
            TextAnchor.Right => t.X - width,
            TextAnchor.Centre => t.X - (width / 2.0),
            _ => t.X,
        };

        // Helvetica's ascenders reach the cap height and its descenders about 0.21 of the size below the baseline.
        return (left, t.Baseline - (t.FontSize * HelveticaMetrics.CapHeight / 1000.0), left + width, t.Baseline + (t.FontSize * 0.21));
    }

    /// <summary>What an item is, in the words a refusal uses.</summary>
    public static string What(SceneItem item) => item switch
    {
        TextRun => "text",
        _ => item.Layer switch
        {
            SceneLayer.Markers => "markers",
            SceneLayer.Codes => "codes",
            SceneLayer.Bulls or SceneLayer.Cells => "bull artwork",
            SceneLayer.DataBlockContent => "codes",
            _ => "rules",
        },
    };

    /// <summary>The margin refusal for one page, or null when every inked item lies inside the printable area.</summary>
    public static string? MarginRefusal(Scene page, PrinterPage printer, int pageNumber = 1, int pageCount = 1)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(printer);
        double paperWidth = page.Width, paperHeight = page.Height;
        var edges = new (string Edge, double Margin, Func<(double Left, double Top, double Right, double Bottom), double> Reach)[]
        {
            ("left", printer.PrintableLeft, e => e.Left),
            ("top", printer.PrintableTop, e => e.Top),
            ("right", paperWidth - printer.PrintableRight, e => paperWidth - e.Right),
            ("bottom", paperHeight - printer.PrintableBottom, e => paperHeight - e.Bottom),
        };

        var found = new List<string>();
        foreach (var (edge, margin, reach) in edges)
        {
            var into = page.Items
                .Select(item => (What: What(item), From: reach(Extent(item))))
                .Where(x => x.From < margin - MarginSlack)
                .ToList();
            if (into.Count == 0)
            {
                continue;
            }

            string what = string.Join(" and ", into.Select(x => x.What).Distinct());
            found.Add(string.Create(CultureInfo.InvariantCulture,
                $"the {edge} edge, where the printer leaves {margin / 20:0.0} mm unprinted and the sheet has {what} {Math.Max(0, into.Min(x => x.From)) / 20:0.0} mm from it"));
        }

        if (found.Count == 0)
        {
            return null;
        }

        string where = pageCount > 1 ? string.Create(CultureInfo.InvariantCulture, $" on sheet {pageNumber} of {pageCount}") : "";
        return $"{printer.Printer} cannot print everything{where}: at {string.Join("; at ", found)}. Part of the sheet would be missing, so nothing "
            + "was printed. Choose a printer with narrower margins, or its borderless setting if it has one.";
    }

    /// <summary>The first refusal over every page to be printed, the paper before the margin, or null when all of them print at actual size.</summary>
    public static string? Refusal(IReadOnlyList<Scene> pages, PrinterPage printer)
    {
        ArgumentNullException.ThrowIfNull(pages);
        for (int i = 0; i < pages.Count; i++)
        {
            if ((PaperRefusal(pages[i], printer) ?? MarginRefusal(pages[i], printer, i + 1, pages.Count)) is { } refusal)
            {
                return refusal;
            }
        }

        return null;
    }

    /// <summary>
    /// The confirmation after <c>EndDoc</c>, entry 107 section 2: the printer, the sheet, the page count and "at actual size". It says the job was
    /// sent to the print queue and nothing more, because GroupLab cannot see whether paper came out.
    /// </summary>
    public static string Confirmation(string printer, string sheet, int pages) => string.Create(CultureInfo.InvariantCulture,
        $"{sheet}, {pages} {(pages == 1 ? "page" : "pages")} at actual size, was sent to the print queue of {printer}.");
}
