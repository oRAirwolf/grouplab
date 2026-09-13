using GroupLab.Core.Imaging;

namespace GroupLab.Core.Rendering;

/// <summary>A rectangle of device pixels, for rasterising part of a page.</summary>
public readonly record struct PixelRegion(int Left, int Top, int Width, int Height);

/// <summary>
/// A second rasteriser of <see cref="Scene"/> pages, independent of any PDF engine. Conformance test 39 of
/// TARGET-SCHEMA.md section 10 requires PDF and raster output to agree within half a device pixel, and with that
/// established, <c>grouplab selftest</c> runs conformance test 43 on its output without a PDF engine. It draws the same
/// integer geometry through the same page transform as <see cref="Pdf.PdfWriter"/>, with true circles where the PDF has
/// Bézier quarters, and draws no text, which positions nothing.
/// </summary>
public static class SceneRasterizer
{
    /// <summary>Samples per axis on a pixel that a circle's edge crosses.</summary>
    private const int Subsamples = 16;

    public static GrayImage Rasterize(Scene page, double dpi, double scale = 1.0, PixelRegion? region = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpi);
        double pixelsPerUnit = dpi / 508.0;
        var r = region ?? new PixelRegion(0, 0, (int)Math.Round(page.Width * pixelsPerUnit), (int)Math.Round(page.Height * pixelsPerUnit));
        var pixels = new byte[(long)r.Width * r.Height];
        Array.Fill(pixels, (byte)255);

        // The content-stream transform of PdfWriter: half-dmm to device pixels, scaled about the page centre.
        double k = scale * pixelsPerUnit;
        double originX = ((1 - scale) * page.Width * pixelsPerUnit / 2) - r.Left;
        double originY = ((1 - scale) * page.Height * pixelsPerUnit / 2) - r.Top;
        var canvas = new Canvas(pixels, r.Width, r.Height);
        foreach (var item in page.Items)
        {
            double luminance = (0.299 * item.Colour.R) + (0.587 * item.Colour.G) + (0.114 * item.Colour.B);
            switch (item)
            {
                case RectFill rect:
                    canvas.FillRect(originX + (rect.X * k), originY + (rect.Y * k), originX + ((rect.X + rect.Width) * k), originY + ((rect.Y + rect.Height) * k), luminance);
                    break;
                case DiscBand band:
                    // A DiscBand radius in half-dmm is the disc's diameter in dmm, so it scales like any other length.
                    canvas.FillBand(originX + (band.CentreX * k), originY + (band.CentreY * k), band.OuterRadius * k, band.InnerRadius * k, luminance);
                    break;
            }
        }

        return new GrayImage(r.Width, r.Height, pixels);
    }

    private readonly record struct Canvas(byte[] Pixels, int Width, int Height)
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

        /// <summary>The ink between two concentric circles: whole pixels decided by distance, edge pixels supersampled.</summary>
        public void FillBand(double cx, double cy, double outer, double inner, double luminance)
        {
            double reach = outer + 1;
            int v0 = Math.Max(0, (int)Math.Floor(cy - reach)), v1 = Math.Min(Height, (int)Math.Ceiling(cy + reach));
            for (int v = v0; v < v1; v++)
            {
                double dy = v + 0.5 - cy;
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
                    Blend((v * Width) + u, Coverage(u, v, cx, cy, d, outer) - Coverage(u, v, cx, cy, d, inner), luminance);
                }
            }
        }

        private static double Coverage(int u, int v, double cx, double cy, double d, double radius)
        {
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
                double sy = v + ((j + 0.5) / Subsamples) - cy;
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
