using GroupLab.Core.Rendering;

namespace GroupLab.Core.Printing.Thermal;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 2a: a target handed to a thermal printer's own app, before GroupLab speaks to the printer itself.
/// Each page drawn as the thermal print mode draws it (one bit, every code and marker on whole dots, black only) at the printer's resolution,
/// exactly the page and nothing round it, as a PNG that carries its resolution and as one PDF of every page at its true size. Printed from the
/// app at 100 percent, never fitted to the page, every dot lands where GroupLab put it.
/// </summary>
public static class PrinterAppFiles
{
    /// <summary>The resolution the files are made at where nothing else says: the Phomemo M834's, and most portable Letter printers'.</summary>
    public const double DefaultDpi = 300;

    /// <summary>The words a file's name carries, so a person can tell these from an office printer's PDF: "thermal-300dpi".</summary>
    public static string Suffix(double dpi) => FormattableString.Invariant($"thermal-{dpi:0}dpi");

    public static (IReadOnlyList<byte[]> Pngs, byte[] Pdf) Make(IReadOnlyList<Scene> pages, double dpi = DefaultDpi)
    {
        ArgumentNullException.ThrowIfNull(pages);
        // A head as wide as anything, so the page is never cut: the app, not GroupLab, knows the paper.
        var head = new PrintHead(dpi, int.MaxValue / 4);
        var printed = pages.Select(p => (Page: p, Print: ThermalRaster.Render(p, head))).ToList();
        return ([.. printed.Select(p => ThermalPng.Write(p.Print.Image, dpi))], ThermalPdf.Write(printed, head));
    }

    /// <summary>
    /// The darkness test (<see cref="DarknessTest"/>) as a whole page, <paramref name="widthInches"/> by <paramref name="heightInches"/>: five
    /// strips down it, numbered by place, so one print shows the same fine detail at the top, middle and bottom of the feed. Through a printer's
    /// app it is printed once at each darkness the app offers, the setting written on it in pen.
    /// </summary>
    public static (IReadOnlyList<byte[]> Pngs, byte[] Pdf) DarknessPage(double widthInches = 8.5, double heightInches = 11, double dpi = DefaultDpi)
    {
        int width = (int)Math.Round(widthInches * dpi), height = (int)Math.Round(heightInches * dpi);
        var image = DarknessTest.Sheet(width, height, 5);
        var page = new Scene((long)Math.Round(widthInches * PrintFit.UnitsPerInch), (long)Math.Round(heightInches * PrintFit.UnitsPerInch), 0, []);
        var head = new PrintHead(dpi, int.MaxValue / 4);
        return ([ThermalPng.Write(image, dpi)], ThermalPdf.Write([(page, new ThermalPage(image, width, 0, null))], head));
    }
}
