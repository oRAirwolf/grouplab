using System.Globalization;
using System.Text;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Rendering;

/// <summary>
/// A page, or part of one, as SVG, NOTES-FROM-PLANNING.md entry 300 section 5: where the site shows a sheet, a bull or a grid, it shows it as
/// vectors from the same scene the PDF is written from, sharp at any size on any screen, instead of a compressed picture. Consecutive items of
/// one ink are one path, as the live preview draws them, which keeps a page of code modules small; the words are the PDF's letters, from the
/// same glyph outlines. Coordinates are the scene's half-dmm, a twentieth of a millimeter, rounded to whole units, which no screen can show.
/// </summary>
public static class SceneSvg
{
    /// <summary>
    /// The page as SVG, cropped to <paramref name="left"/>, <paramref name="top"/>, <paramref name="right"/>, <paramref name="bottom"/> in dmm
    /// where given, drawn <paramref name="pixelsWide"/> wide by default, on white paper.
    /// </summary>
    public static string Write(Scene page, double? left = null, double? top = null, double? right = null, double? bottom = null, int pixelsWide = 800)
    {
        ArgumentNullException.ThrowIfNull(page);
        double x0 = 2 * Math.Max(0, left ?? 0), y0 = 2 * Math.Max(0, top ?? 0);
        double x1 = Math.Min(page.Width, 2 * (right ?? page.Width / 2.0)), y1 = Math.Min(page.Height, 2 * (bottom ?? page.Height / 2.0));
        double w = Math.Max(1, x1 - x0), h = Math.Max(1, y1 - y0);
        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{N(x0)} {N(y0)} {N(w)} {N(h)}\" width=\"{pixelsWide}\" height=\"{(int)Math.Round(pixelsWide * h / w)}\">");
        svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{N(x0)}\" y=\"{N(y0)}\" width=\"{N(w)}\" height=\"{N(h)}\" fill=\"#fff\"/>");
        var path = new StringBuilder();
        Rgb? ink = null;
        bool? band = null;
        void Flush()
        {
            if (path.Length > 0 && ink is { } colour)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<path fill=\"{colour}\"{(band == true ? " fill-rule=\"evenodd\"" : "")} d=\"{path}\"/>");
            }

            path.Clear();
        }

        foreach (var item in page.Items)
        {
            if (item is not (DiscBand or RectFill or TextRun) || !Near(item, x0, y0, x1, y1))
            {
                continue;
            }

            bool isBand = item is DiscBand;
            if (ink != item.Colour || band != isBand)
            {
                Flush();
                (ink, band) = (item.Colour, isBand);
            }

            switch (item)
            {
                case DiscBand disc:
                    Outline(path, disc.CentreX, disc.CentreY, disc.Outer);
                    if (disc.InnerRadius > 0)
                    {
                        Outline(path, disc.CentreX, disc.CentreY, disc.Inner);
                    }

                    break;
                case RectFill rect:
                    path.Append(CultureInfo.InvariantCulture, $"M{N(rect.X)} {N(rect.Y)}h{N(rect.Width)}v{N(rect.Height)}h{N(-rect.Width)}z");
                    break;
                case TextRun text:
                    foreach (var contour in SheetGlyphs.Contours(text))
                    {
                        Polygon(path, contour);
                    }

                    break;
            }
        }

        Flush();
        svg.Append("</svg>");
        return svg.ToString();
    }

    /// <summary>Whether an item can reach into the crop at all.</summary>
    private static bool Near(SceneItem item, double x0, double y0, double x1, double y1) => item switch
    {
        DiscBand d => d.CentreX + d.OuterRadius >= x0 && d.CentreX - d.OuterRadius <= x1 && d.CentreY + d.OuterRadius >= y0 && d.CentreY - d.OuterRadius <= y1,
        RectFill r => r.X + r.Width >= x0 && r.X <= x1 && r.Y + r.Height >= y0 && r.Y <= y1,
        TextRun t => SheetGlyphs.Contours(t).SelectMany(c => c).ToList() is { Count: > 0 } points
            && points.Max(p => p.X) >= x0 && points.Min(p => p.X) <= x1 && points.Max(p => p.Y) >= y0 && points.Min(p => p.Y) <= y1,
        _ => true,
    };

    private static void Outline(StringBuilder path, double x, double y, Outline outline)
    {
        if (outline.IsCircle)
        {
            double r = outline.Radius;
            path.Append(CultureInfo.InvariantCulture, $"M{N(x + r)} {N(y)}A{N(r)} {N(r)} 0 1 1 {N(x - r)} {N(y)}A{N(r)} {N(r)} 0 1 1 {N(x + r)} {N(y)}z");
            return;
        }

        Polygon(path, [.. outline.Corners().Select(c => (x + c.X, y + c.Y))]);
    }

    private static void Polygon(StringBuilder path, IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 3)
        {
            return;
        }

        path.Append(CultureInfo.InvariantCulture, $"M{N(points[0].X)} {N(points[0].Y)}");
        for (int i = 1; i < points.Count; i++)
        {
            path.Append(CultureInfo.InvariantCulture, $"L{N(points[i].X)} {N(points[i].Y)}");
        }

        path.Append('z');
    }

    private static string N(double v) => Math.Round(v).ToString("0", CultureInfo.InvariantCulture);
}
