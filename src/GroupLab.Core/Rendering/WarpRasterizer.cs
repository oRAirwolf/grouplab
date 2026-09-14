using GroupLab.Core.Imaging;

namespace GroupLab.Core.Rendering;

/// <summary>
/// A page render resampled through a known map from page to image, so a synthetic photograph of a bent sheet has exact
/// truth (PHASE1-BRIEF.md section 3.3). The page is cut into a mesh of small squares, each square's corners are sent through
/// the map, and every image pixel inside each resulting triangle takes the page point its barycentric coordinates give,
/// sampled bilinearly from the render. At the default step a triangle is about a millimetre of paper, where the map is
/// linear to far below a pixel.
/// </summary>
public static class WarpRasterizer
{
    /// <param name="page">The page rendered at <paramref name="dpi"/>, with pixel (u, v) at (u, v).</param>
    /// <param name="pageToImage">Page dmm to image pixel; a NaN coordinate leaves that part of the page undrawn.</param>
    public static GrayImage Render(GrayImage page, double dpi, Func<PointD, PointD> pageToImage, int width, int height,
        double pageWidth, double pageHeight, double stepDmm = 10, byte background = 180)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(pageToImage);
        var pixels = new byte[width * height];
        Array.Fill(pixels, background);
        int cols = (int)Math.Ceiling(pageWidth / stepDmm), rows = (int)Math.Ceiling(pageHeight / stepDmm);
        var pagePoints = new PointD[(cols + 1) * (rows + 1)];
        var imagePoints = new PointD[pagePoints.Length];
        for (int j = 0; j <= rows; j++)
        {
            for (int i = 0; i <= cols; i++)
            {
                var p = new PointD(Math.Min(i * stepDmm, pageWidth), Math.Min(j * stepDmm, pageHeight));
                pagePoints[(j * (cols + 1)) + i] = p;
                imagePoints[(j * (cols + 1)) + i] = pageToImage(p);
            }
        }

        double toRender = dpi / 254;
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < cols; i++)
            {
                int a = (j * (cols + 1)) + i, b = a + 1, c = a + cols + 1, d = c + 1;
                Triangle(a, b, d);
                Triangle(a, d, c);
            }
        }

        return new GrayImage(width, height, pixels);

        void Triangle(int ia, int ib, int ic)
        {
            PointD a = imagePoints[ia], b = imagePoints[ib], c = imagePoints[ic];
            if (double.IsNaN(a.X) || double.IsNaN(b.X) || double.IsNaN(c.X))
            {
                return;
            }

            double det = ((b.Y - c.Y) * (a.X - c.X)) + ((c.X - b.X) * (a.Y - c.Y));
            if (Math.Abs(det) < 1e-12)
            {
                return;
            }

            int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
            int x1 = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
            int y0 = Math.Max(0, (int)Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))));
            int y1 = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))));
            PointD pa = pagePoints[ia], pb = pagePoints[ib], pc = pagePoints[ic];
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double wa = (((b.Y - c.Y) * (x - c.X)) + ((c.X - b.X) * (y - c.Y))) / det;
                    double wb = (((c.Y - a.Y) * (x - c.X)) + ((a.X - c.X) * (y - c.Y))) / det;
                    double wc = 1 - wa - wb;
                    if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9)
                    {
                        continue;
                    }

                    double px = (wa * pa.X) + (wb * pb.X) + (wc * pc.X), py = (wa * pa.Y) + (wb * pb.Y) + (wc * pc.Y);
                    pixels[(y * width) + x] = Sample(page, (px * toRender) - 0.5, (py * toRender) - 0.5);
                }
            }
        }
    }

    private static byte Sample(GrayImage image, double x, double y)
    {
        if (x < 0 || y < 0 || x > image.Width - 1 || y > image.Height - 1)
        {
            return 255;
        }

        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        int w = image.Width;
        double value = ((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])));
        return (byte)Math.Clamp(Math.Round(value), 0, 255);
    }
}
