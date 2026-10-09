using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Rendering;

/// <summary>A rectangle of device pixels, for rasterising part of a page.</summary>
public readonly record struct PixelRegion(int Left, int Top, int Width, int Height);

/// <summary>
/// A second rasteriser of <see cref="Scene"/> pages, independent of any PDF engine. Conformance test 39 of
/// TARGET-SCHEMA.md section 10 requires PDF and raster output to agree within half a device pixel, and with that
/// established, <c>grouplab selftest</c> runs conformance test 43 on its output without a PDF engine. It draws the same
/// integer geometry through the same page transform as <see cref="Pdf.PdfWriter"/>, with true circles where the PDF has
/// Bézier quarters, and draws no text unless asked, which positions nothing.
/// <para>
/// NOTES-FROM-PLANNING.md entry 250 section 1: the Targets screen's preview asks for the words as well, drawn from <see cref="SheetGlyphs"/> at
/// the PDF's own positions, so it is the sheet as it prints. Everything that measures a render leaves them out, as it always has.
/// </para>
/// </summary>
public static class SceneRasterizer
{
    /// <summary>Samples per axis on a pixel that a circle's edge crosses.</summary>
    private const int Subsamples = 16;

    /// <summary>An ink's level by its luminance, the default, as a grey photograph sees it.</summary>
    public static double Luminance(Gltd.Binary.Rgb ink) => (0.299 * ink.R) + (0.587 * ink.G) + (0.114 * ink.B);

    /// <summary>
    /// An ink's level as HSV value, max(R, G, B), which is how the hole finder sees a photograph (entry 297): black and grey are the same
    /// as by luminance, and a red or blue bull is light, so a hole shows dark against it.
    /// </summary>
    public static double Value(Gltd.Binary.Rgb ink) => Math.Max(ink.R, Math.Max(ink.G, ink.B));

    /// <summary>
    /// The page in color, as three channels interleaved blue, green, red for an image encoder, for a preview that shows a sheet's bulls in
    /// their color (entry 297 section 2).
    /// </summary>
    public static (int Width, int Height, byte[] Bgr) RasterizeBgr(Scene page, double dpi, bool words = false)
    {
        var b = Rasterize(page, dpi, words: words, level: ink => ink.B);
        var g = Rasterize(page, dpi, words: words, level: ink => ink.G);
        var r = Rasterize(page, dpi, words: words, level: ink => ink.R);
        var bgr = new byte[b.Pixels.Length * 3];
        for (int i = 0; i < b.Pixels.Length; i++)
        {
            bgr[3 * i] = b.Pixels[i];
            bgr[(3 * i) + 1] = g.Pixels[i];
            bgr[(3 * i) + 2] = r.Pixels[i];
        }

        return (b.Width, b.Height, bgr);
    }

    /// <param name="level">An ink's level, 0 to 255; by its luminance where not given.</param>
    /// <param name="stretchY">Rows per row down the page, for a printer that feeds short (entry 391); 1 everywhere else.</param>
    public static GrayImage Rasterize(Scene page, double dpi, double scale = 1.0, PixelRegion? region = null, bool words = false, Func<Gltd.Binary.Rgb, double>? level = null,
        double stretchY = 1.0)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpi);
        double pixelsPerUnit = dpi / 508.0;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stretchY);
        var r = region ?? new PixelRegion(0, 0, (int)Math.Round(page.Width * pixelsPerUnit), (int)Math.Round(page.Height * pixelsPerUnit * stretchY));
        var pixels = new byte[(long)r.Width * r.Height];
        Array.Fill(pixels, (byte)255);

        // The content-stream transform of PdfWriter: half-dmm to device pixels, scaled about the page centre.
        double k = scale * pixelsPerUnit, ky = k * stretchY;
        double originX = ((1 - scale) * page.Width * pixelsPerUnit / 2) - r.Left;
        double originY = ((1 - scale) * page.Height * pixelsPerUnit * stretchY / 2) - r.Top;
        var canvas = new Canvas(pixels, r.Width, r.Height, stretchY);
        foreach (var item in page.Items)
        {
            double luminance = (level ?? Luminance)(item.Colour);
            switch (item)
            {
                case RectFill rect:
                    canvas.FillRect(originX + (rect.X * k), originY + (rect.Y * ky), originX + ((rect.X + rect.Width) * k), originY + ((rect.Y + rect.Height) * ky), luminance);
                    break;
                case DiscBand band:
                    // A DiscBand radius in half-dmm is the disc's diameter in dmm, so it scales like any other length.
                    canvas.FillBand(originX + (band.CentreX * k), originY + (band.CentreY * ky), band.Outer with { Radius = band.OuterRadius * k },
                        band.Inner with { Radius = band.InnerRadius * k }, luminance);
                    break;
                case TextRun text when words:
                    canvas.FillContours([.. SheetGlyphs.Contours(text).Select(c => c.Select(p => (originX + (p.X * k), originY + (p.Y * ky))).ToArray())], luminance);
                    break;
            }
        }

        return new GrayImage(r.Width, r.Height, pixels);
    }

    /// <param name="StretchY">Rows per row down the page: a band is decided in the unstretched frame, so a circle prints as one, stretched.</param>
    private readonly record struct Canvas(byte[] Pixels, int Width, int Height, double StretchY = 1.0)
    {
        /// <summary>Exact area coverage of an axis-aligned rectangle.</summary>
        public void FillRect(double x0, double y0, double x1, double y1, double luminance)
        {
            int u0 = Math.Max(0, (int)Math.Floor(x0)), u1 = Math.Min(Width, (int)Math.Ceiling(x1));
            int v0 = Math.Max(0, (int)Math.Floor(y0)), v1 = Math.Min(Height, (int)Math.Ceiling(y1));
            for (int v = v0; v < v1; v++)
            {
                double cy = Math.Min(v + 1, y1) - Math.Max(v, y0);
                for (int u = u0; u < u1; u++)
                {
                    Blend((v * Width) + u, (Math.Min(u + 1, x1) - Math.Max(u, x0)) * cy, luminance);
                }
            }
        }

        /// <summary>
        /// The ink between two concentric outlines: whole pixels decided by distance, edge pixels supersampled. A circle is exactly as it
        /// always was; a square (entry 243 section 4) is decided by its distance to the nearer side, which is exact inside it and never
        /// more than the true distance outside, so a pixel it calls wholly outside is.
        /// </summary>
        public void FillBand(double cx, double cy, Outline outer, Outline inner, double luminance)
        {
            double reach = outer.Radius + 1, rows = reach * StretchY;
            int v0 = Math.Max(0, (int)Math.Floor(cy - rows)), v1 = Math.Min(Height, (int)Math.Ceiling(cy + rows));
            for (int v = v0; v < v1; v++)
            {
                // Entry 391: down the page in the unstretched frame, so a pixel there is no more than 0.71 of one from its corners.
                double dy = (v + 0.5 - cy) / StretchY;
                if (Math.Abs(dy) >= reach)
                {
                    continue;
                }

                double half = Math.Sqrt((reach * reach) - (dy * dy));
                int u0 = Math.Max(0, (int)Math.Floor(cx - half)), u1 = Math.Min(Width, (int)Math.Ceiling(cx + half));
                for (int u = u0; u < u1; u++)
                {
                    double dx = u + 0.5 - cx;
                    double d = Math.Sqrt((dx * dx) + (dy * dy));
                    Blend((v * Width) + u, Coverage(u, v, cx, cy, dx, dy, d, outer, StretchY) - Coverage(u, v, cx, cy, dx, dy, d, inner, StretchY), luminance);
                }
            }
        }

        private static double Coverage(int u, int v, double cx, double cy, double dx, double dy, double d, Outline outline, double stretchY)
        {
            if (!outline.IsCircle)
            {
                return SquareCoverage(u, v, cx, cy, dx, dy, outline, stretchY);
            }

            double radius = outline.Radius;
            // A pixel's corners are within 0.71 px of its centre, so outside this band it is wholly in or out.
            if (radius <= 0 || d >= radius + 0.75)
            {
                return 0;
            }

            if (d <= radius - 0.75)
            {
                return 1;
            }

            int inside = 0;
            double r2 = radius * radius;
            for (int j = 0; j < Subsamples; j++)
            {
                double sy = (v + ((j + 0.5) / Subsamples) - cy) / stretchY;
                for (int i = 0; i < Subsamples; i++)
                {
                    double sx = u + ((i + 0.5) / Subsamples) - cx;
                    if ((sx * sx) + (sy * sy) <= r2)
                    {
                        inside++;
                    }
                }
            }

            return inside / (double)(Subsamples * Subsamples);
        }

        private static double SquareCoverage(int u, int v, double cx, double cy, double dx, double dy, Outline square, double stretchY)
        {
            if (square.Radius <= 0)
            {
                return 0;
            }

            double distance = square.Distance(dx, dy, out _, out _);
            if (distance >= 0.75)
            {
                return 0;
            }

            if (distance <= -0.75)
            {
                return 1;
            }

            int inside = 0;
            for (int j = 0; j < Subsamples; j++)
            {
                double sy = (v + ((j + 0.5) / Subsamples) - cy) / stretchY;
                for (int i = 0; i < Subsamples; i++)
                {
                    double sx = u + ((i + 0.5) / Subsamples) - cx;
                    if (square.Distance(sx, sy, out _, out _) <= 0)
                    {
                        inside++;
                    }
                }
            }

            return inside / (double)(Subsamples * Subsamples);
        }

        /// <summary>
        /// Closed outlines filled by the nonzero rule, as a PDF fills a glyph: each pixel row is sampled on <see cref="Subsamples"/> lines,
        /// and on each line the covered length of every pixel is exact.
        /// </summary>
        public void FillContours(IReadOnlyList<(double X, double Y)[]> contours, double luminance)
        {
            if (contours.Count == 0)
            {
                return;
            }

            double top = contours.Min(c => c.Min(p => p.Y)), bottom = contours.Max(c => c.Max(p => p.Y));
            double left = contours.Min(c => c.Min(p => p.X)), right = contours.Max(c => c.Max(p => p.X));
            int v0 = Math.Max(0, (int)Math.Floor(top)), v1 = Math.Min(Height, (int)Math.Ceiling(bottom));
            int u0 = Math.Max(0, (int)Math.Floor(left)), u1 = Math.Min(Width, (int)Math.Ceiling(right));
            if (v0 >= v1 || u0 >= u1)
            {
                return;
            }

            var cover = new double[u1 - u0];
            var crossings = new List<(double X, int Winding)>();
            for (int v = v0; v < v1; v++)
            {
                Array.Clear(cover);
                for (int j = 0; j < Subsamples; j++)
                {
                    double y = v + ((j + 0.5) / Subsamples);
                    crossings.Clear();
                    foreach (var contour in contours)
                    {
                        for (int i = 0; i < contour.Length; i++)
                        {
                            var a = contour[i];
                            var b = contour[(i + 1) % contour.Length];
                            if ((a.Y <= y) == (b.Y <= y))
                            {
                                continue;
                            }

                            crossings.Add((a.X + ((y - a.Y) / (b.Y - a.Y) * (b.X - a.X)), b.Y > a.Y ? 1 : -1));
                        }
                    }

                    crossings.Sort((p, q) => p.X.CompareTo(q.X));
                    int winding = 0;
                    for (int i = 0; i + 1 < crossings.Count; i++)
                    {
                        winding += crossings[i].Winding;
                        if (winding != 0)
                        {
                            Span(cover, u0, crossings[i].X, crossings[i + 1].X);
                        }
                    }
                }

                for (int u = u0; u < u1; u++)
                {
                    Blend((v * Width) + u, Math.Min(1, cover[u - u0] / Subsamples), luminance);
                }
            }
        }

        /// <summary>Adds the length of [x0, x1] that falls in each pixel of the row to its coverage.</summary>
        private static void Span(double[] cover, int u0, double x0, double x1)
        {
            int first = Math.Max(u0, (int)Math.Floor(x0)), last = Math.Min(u0 + cover.Length - 1, (int)Math.Floor(x1));
            for (int u = first; u <= last; u++)
            {
                cover[u - u0] += Math.Max(0, Math.Min(u + 1, x1) - Math.Max(u, x0));
            }
        }

        private void Blend(int index, double coverage, double luminance)
        {
            if (coverage <= 0)
            {
                return;
            }

            double value = (Pixels[index] * (1 - coverage)) + (luminance * coverage);
            Pixels[index] = (byte)Math.Clamp(Math.Round(value), 0, 255);
        }
    }
}
