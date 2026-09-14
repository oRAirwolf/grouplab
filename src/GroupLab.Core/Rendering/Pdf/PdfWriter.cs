using System.Globalization;
using System.Text;

namespace GroupLab.Core.Rendering.Pdf;

/// <summary>
/// A minimal PDF 1.7 writer for <see cref="Scene"/> pages. It writes one coordinate transform per page, from half-dmm
/// with the origin at the top-left (TARGET-SCHEMA.md section 3.2) to PDF points with the origin at the bottom-left,
/// and after that every path coordinate is the scene's integer. There are no strokes, only fills: an annulus is two
/// circles filled with the even-odd rule, which is the disc-stack construction of section 3.4 and leaves a graphics
/// stack nothing to interpret. Output is deterministic, with no timestamps and uncompressed content (conformance test 38).
/// </summary>
public static class PdfWriter
{
    /// <summary>Points per half-dmm: 72 points per inch over 508 half-dmm per inch.</summary>
    public const double PointsPerUnit = 72.0 / 508.0;

    /// <summary>
    /// The control-point ratio of a quarter circle drawn as one cubic Bézier that minimises the largest radial error,
    /// about 0.02 percent of the radius; each quarter keeps its end points on the integer axes of the circle.
    /// </summary>
    private const double Kappa = 0.5519150244935105707;

    public static byte[] Write(IReadOnlyList<Scene> pages, double scale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(pages);
        if (pages.Count == 0)
        {
            throw new ArgumentException("A PDF needs at least one page.", nameof(pages));
        }

        var objects = new List<string>
        {
            // NOTES-FROM-PLANNING.md entry 25 section 2: ask the viewer to print with no scaling. A viewer may ignore it and a driver
            // can still shrink the page, which is why the print screen also says so and the sheet can carry a printed note.
            "<< /Type /Catalog /Pages 2 0 R /ViewerPreferences << /PrintScaling /None >> >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pages.Select((_, i) => $"{4 + (2 * i)} 0 R"))}] /Count {pages.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
        };

        for (int i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            string content = Content(page, scale);
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(page.Width * PointsPerUnit)} {Number(page.Height * PointsPerUnit)}] " +
                $"/Resources << /Font << /F1 3 0 R >> >> /Contents {5 + (2 * i)} 0 R >>");
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        var pdf = new StringBuilder("%PDF-1.7\n%âãÏÓ\n");
        var offsets = new List<int>();
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        int xref = Encoding.Latin1.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    /// <summary>The content stream of one page. Exposed so tests can inspect the operators actually written.</summary>
    public static string Content(Scene page, double scale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(page);
        double widthPoints = page.Width * PointsPerUnit, heightPoints = page.Height * PointsPerUnit;
        var s = new StringBuilder();

        // Maps (x, y) in half-dmm from the top-left to points from the bottom-left, scaled about the page centre.
        s.Append(CultureInfo.InvariantCulture,
            $"q\n{Number(scale * PointsPerUnit)} 0 0 {Number(-scale * PointsPerUnit)} {Number((1 - scale) * widthPoints / 2)} {Number((1 + scale) * heightPoints / 2)} cm\n");

        Gltd.Binary.Rgb? colour = null;
        bool pendingRects = false;
        foreach (var item in page.Items)
        {
            if (item is not RectFill && pendingRects)
            {
                s.Append("f\n");
                pendingRects = false;
            }

            if (colour != item.Colour)
            {
                if (pendingRects)
                {
                    s.Append("f\n");
                    pendingRects = false;
                }

                s.Append(CultureInfo.InvariantCulture, $"{Channel(item.Colour.R)} {Channel(item.Colour.G)} {Channel(item.Colour.B)} rg\n");
                colour = item.Colour;
            }

            switch (item)
            {
                case RectFill r:
                    s.Append(CultureInfo.InvariantCulture, $"{r.X} {r.Y} {r.Width} {r.Height} re\n");
                    pendingRects = true;
                    break;
                case DiscBand band:
                    Circle(s, band.CentreX, band.CentreY, band.OuterRadius);
                    if (band.InnerRadius > 0)
                    {
                        Circle(s, band.CentreX, band.CentreY, band.InnerRadius);
                        s.Append("f*\n");
                    }
                    else
                    {
                        s.Append("f\n");
                    }

                    break;
                case TextRun text:
                    Text(s, text);
                    break;
            }
        }

        if (pendingRects)
        {
            s.Append("f\n");
        }

        s.Append('Q');
        return s.ToString();
    }

    private static void Circle(StringBuilder s, long cx, long cy, long r)
    {
        double kr = r * Kappa;
        s.Append(CultureInfo.InvariantCulture, $"{cx + r} {cy} m\n");
        s.Append(CultureInfo.InvariantCulture, $"{cx + r} {Number(cy + kr)} {Number(cx + kr)} {cy + r} {cx} {cy + r} c\n");
        s.Append(CultureInfo.InvariantCulture, $"{Number(cx - kr)} {cy + r} {cx - r} {Number(cy + kr)} {cx - r} {cy} c\n");
        s.Append(CultureInfo.InvariantCulture, $"{cx - r} {Number(cy - kr)} {Number(cx - kr)} {cy - r} {cx} {cy - r} c\n");
        s.Append(CultureInfo.InvariantCulture, $"{Number(cx + kr)} {cy - r} {cx + r} {Number(cy - kr)} {cx + r} {cy} c\nh\n");
    }

    private static void Text(StringBuilder s, TextRun text)
    {
        long width = HelveticaMetrics.TextWidth(text.Text, text.FontSize);
        double x = text.Anchor switch
        {
            TextAnchor.Right => text.X - width,
            TextAnchor.Centre => text.X - (width / 2.0),
            _ => text.X,
        };
        var escaped = new StringBuilder();
        foreach (char c in text.Text)
        {
            char mapped = HelveticaMetrics.ToWinAnsi(c);
            if (mapped is '(' or ')' or '\\')
            {
                escaped.Append('\\');
            }

            escaped.Append(mapped);
        }

        // The page transform flips y, so the text matrix flips it back to keep glyphs upright.
        s.Append(CultureInfo.InvariantCulture, $"BT /F1 {text.FontSize} Tf 1 0 0 -1 {Number(x)} {text.Baseline} Tm ({escaped}) Tj ET\n");
    }

    private static string Channel(byte value) => Number(value / 255.0);

    private static string Number(double value) =>
        Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
}
