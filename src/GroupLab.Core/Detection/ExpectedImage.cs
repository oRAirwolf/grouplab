using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Detection;

/// <summary>
/// Stage S5 of docs/DETECTION-PIPELINE.md, the geometric half: a page render resampled into image space through a
/// registration, so the expected artwork lies on the observed image pixel for pixel. The render is
/// <see cref="Rendering.SceneRasterizer"/>'s, whose pixel u covers page [u, u + 1) at its resolution, so the page point
/// under render pixel u's centre is (u + 0.5) times 254 over the resolution; image pixels here are addressed by their
/// centres, as every other image coordinate in the pipeline is. The mapping is evaluated on a grid every
/// <see cref="GridStep"/> pixels and interpolated between, which is exact for a scan's affine registration and far below a
/// pixel for a photograph's. Outside the page the image is paper.
/// </summary>
public static class ExpectedImage
{
    public const int GridStep = 8;

    public static GrayImage Render(GrayImage render, double renderDpi, IPageMapping mapping, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(mapping);
        int gw = (width / GridStep) + 2, gh = (height / GridStep) + 2;
        var rx = new double[gw * gh];
        var ry = new double[gw * gh];
        double toRender = renderDpi / 254;
        for (int j = 0; j < gh; j++)
        {
            for (int i = 0; i < gw; i++)
            {
                var page = mapping.ToPage(new PointD(i * GridStep, j * GridStep));
                rx[(j * gw) + i] = (page.X * toRender) - 0.5;
                ry[(j * gw) + i] = (page.Y * toRender) - 0.5;
            }
        }

        var pixels = new byte[width * height];
        Parallel.For(0, height, v =>
        {
            int j = v / GridStep;
            double fy = (v - (j * GridStep)) / (double)GridStep;
            int row = v * width;
            for (int u = 0; u < width; u++)
            {
                int i = u / GridStep;
                double fx = (u - (i * GridStep)) / (double)GridStep;
                int a = (j * gw) + i, b = a + 1, c = a + gw, d = c + 1;
                double x = ((1 - fy) * (((1 - fx) * rx[a]) + (fx * rx[b]))) + (fy * (((1 - fx) * rx[c]) + (fx * rx[d])));
                double y = ((1 - fy) * (((1 - fx) * ry[a]) + (fx * ry[b]))) + (fy * (((1 - fx) * ry[c]) + (fx * ry[d])));
                pixels[row + u] = Sample(render, x, y);
            }
        });

        return new GrayImage(width, height, pixels);
    }

    /// <summary>Bilinear between render pixel centres; paper beyond the page's edge.</summary>
    private static byte Sample(GrayImage render, double x, double y)
    {
        if (!(x >= -0.5 && y >= -0.5 && x <= render.Width - 0.5 && y <= render.Height - 0.5))
        {
            return 255;
        }

        x = Math.Clamp(x, 0, render.Width - 1);
        y = Math.Clamp(y, 0, render.Height - 1);
        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, render.Width - 1), y1 = Math.Min(y0 + 1, render.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = render.Pixels;
        int w = render.Width;
        double value = ((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])));
        return (byte)Math.Clamp(Math.Round(value), 0, 255);
    }
}
