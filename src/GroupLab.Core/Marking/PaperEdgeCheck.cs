using System.Globalization;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Marking;

/// <summary>What the paper's own edge says about how large a sheet was printed.</summary>
/// <param name="Across">The print scale across the sheet the paper's edge implies: its known width over its width in the sheet's own units.</param>
/// <param name="Down">The same, down the sheet.</param>
/// <param name="Paper">The paper it was taken to be, "Letter" or "A4", from the shape of its edge.</param>
public sealed record PaperEdge(double Across, double Down, string Paper)
{
    /// <summary>The one figure for the area.</summary>
    public double Scale => Math.Sqrt(Across * Down);
}

/// <summary>
/// The paper-edge check on every photograph, NOTES-FROM-PLANNING.md entries 272 and 273 section 5. A photograph has no absolute ruler, but
/// the paper a sheet is printed on has a known size: its four edges, found by <see cref="SheetOutline"/> and carried onto the page through
/// the markers' registration, measure the paper in the sheet's own units, and the paper's true size over that is the print scale. It is a
/// check, not a measurement to correct by: the paper's edge is outside the markers, the edge is found to a pixel or two, and a trimmed or
/// torn sheet reads wrong. So it only warns: when it disagrees with the printer profile by more than <see cref="Disagrees"/>, or when there
/// is no profile and the sheet looks printed with Fit to page.
/// <para>
/// Which paper: a Letter sheet printed on A4 at its true size is clipped, and one printed with Fit to page is smaller by the same amount both
/// ways, so the paper whose two implied scales agree best is the paper it was. Letter is 0.773 as wide as it is tall and A4 0.707, which
/// the edge tells apart easily.
/// </para>
/// </summary>
public static class PaperEdgeCheck
{
    /// <summary>How far the paper's edge may disagree with the printer profile before GroupLab says so (entry 272: about 1.5 percent).</summary>
    public const double Disagrees = 0.015;

    /// <summary>Below this, with no profile, a sheet looks printed with Fit to page, which shrinks it by 3 to 6 percent.</summary>
    public const double LooksFitToPage = 0.985;

    /// <summary>How far the two opposite edges may differ before the edge is not believed: a curled, trimmed or folded sheet.</summary>
    private const double SidesAgree = 0.02;

    private static readonly (string Name, double Width, double Height)[] Papers = [("Letter", 2159, 2794), ("A4", 2100, 2970)];

    /// <summary>What the paper's edge says, or null where no clear edge was found or what it says is beyond belief.</summary>
    public static PaperEdge? Measure(GrayImage image, IPageMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(mapping);
        if (SheetOutline.Find(image, out _) is not { } quad)
        {
            return null;
        }

        var c = quad.Corners.Select(mapping.ToPage).ToArray();
        double top = Distance(c[0], c[1]), bottom = Distance(c[3], c[2]), left = Distance(c[0], c[3]), right = Distance(c[1], c[2]);
        if (Math.Abs(top - bottom) > SidesAgree * Math.Max(top, bottom) || Math.Abs(left - right) > SidesAgree * Math.Max(left, right))
        {
            return null;
        }

        double across = (top + bottom) / 2, down = (left + right) / 2;
        // A sheet photographed sideways still has its page axes; only a landscape definition would swap them, and the paper is then wider.
        var best = Papers
            .Select(p => across > down ? (p.Name, Width: p.Height, Height: p.Width) : p)
            .Select(p => new PaperEdge(p.Width / across, p.Height / down, p.Name))
            .MinBy(e => Math.Abs(e.Across - e.Down))!;
        return best.Across is < SheetReference.LowestBelievable or > SheetReference.HighestBelievable
            || best.Down is < SheetReference.LowestBelievable or > SheetReference.HighestBelievable
            || Math.Abs(best.Across - best.Down) > SidesAgree
            ? null
            : best;
    }

    /// <summary>
    /// What the paper's edge adds to a photograph's result, or null where there is nothing to say: it agrees with the printer's profile, or
    /// there is none and the sheet looks printed at its true size.
    /// </summary>
    public static string? Advice(PaperEdge? edge, PrinterProfile? printer)
    {
        if (edge is null)
        {
            return null;
        }

        var inv = CultureInfo.InvariantCulture;
        if (printer is not null)
        {
            return Math.Abs(edge.Across - printer.Across) > Disagrees || Math.Abs(edge.Down - printer.Down) > Disagrees
                ? string.Create(inv, $"The paper's edge says this sheet was printed at {edge.Across * 100:0.0} by {edge.Down * 100:0.0}%, which disagrees with {printer.Name}'s {printer.Percentages}. It may have come from another printer or another setting; check it again under Printers in Settings.")
                : null;
        }

        return edge.Scale < LooksFitToPage
            ? string.Create(inv, $"The paper's edge says this sheet was printed at about {edge.Scale * 100:0.0}%, which looks like Fit to page. Its figures are in the sheet's own inches; check your printer, under Printers in Settings, for real inches.")
            : null;
    }

    /// <summary>The wizard's line (entry 273): whether the paper's edge agrees with a profile just measured, and by how much.</summary>
    public static string Agreement(PaperEdge edge, PrinterProfile printer)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(printer);
        double off = Math.Max(Math.Abs(edge.Across - printer.Across), Math.Abs(edge.Down - printer.Down));
        return string.Create(CultureInfo.InvariantCulture, $"{(off <= Disagrees ? "agrees" : "disagrees")}, within {Math.Ceiling(off * 1000) / 10:0.0}%");
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
}
