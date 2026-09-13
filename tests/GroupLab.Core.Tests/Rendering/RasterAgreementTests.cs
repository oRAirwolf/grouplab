using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// Conformance test 39 of TARGET-SCHEMA.md section 10: rendering to PDF and to a 600 DPI raster produces geometry
/// agreeing within half a device pixel. The PDF is rasterised by PDFium and the raster comes from
/// <see cref="SceneRasterizer"/>, two renderers that share nothing but the scene. Along profiles through bulls and
/// markers, every 50 percent ink crossing in one must pair with a crossing in the other.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class RasterAgreementTests(ITestOutputHelper output)
{
    private const int Dpi = 600;
    private const double Tolerance = 0.5;
    private const double Step = 0.05;

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string file in BuiltIns.Files)
        {
            data.Add(file);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Test39PdfAndRasterGeometryAgreeWithinHalfAPixel(string file)
    {
        var d = BuiltIns.Load(file);
        var page = TargetRenderer.Render(d).Pages[0];
        var failures = new List<string>();
        double worst = 0;

        foreach (int index in new[] { 0, d.Bulls.Count - 1 }.Distinct())
        {
            var bull = d.Bulls[index];
            double outer = page.Items.OfType<DiscBand>().Where(b => b.CentreX == 2L * bull.X && b.CentreY == 2L * bull.Y).Max(b => b.OuterRadius) / 2.0;
            var (pdf, raster) = Pair(page, bull.X, bull.Y, (2 * outer) + 40);

            // On a zeroing sheet the grid's axes cross the aiming mark; rays along them would measure the grid.
            bool gridThroughBull = d.Grids?.Any(g => g.CentreX == bull.X && g.CentreY == bull.Y) ?? false;
            for (int n = 0; n < 16; n++)
            {
                if (gridThroughBull && n % 4 == 0)
                {
                    continue;
                }

                double angle = n * Math.PI / 8;
                Compare($"bull {index} at {n * 22.5:0.#} deg", pdf, raster, bull.X, bull.Y, Math.Cos(angle), Math.Sin(angle), outer + 10, failures, ref worst);
            }
        }

        var markers = PageRegistration.ExpectedMarkers(d, page.TileIndex);
        foreach (var marker in new[] { markers[0], markers[^1] }.Distinct())
        {
            double size = d.Fiducials!.MarkerSize, module = size / 8;
            var (pdf, raster) = Pair(page, marker.X, marker.Y, size + 40);
            for (int k = 0; k < 8; k++)
            {
                double offset = (-size / 2) + ((k + 0.5) * module);
                Compare($"marker {marker.Id} row {k}", pdf, raster, marker.X - (size / 2) - module, marker.Y + offset, 1, 0, size + (2 * module), failures, ref worst);
                Compare($"marker {marker.Id} column {k}", pdf, raster, marker.X + offset, marker.Y - (size / 2) - module, 0, 1, size + (2 * module), failures, ref worst);
            }
        }

        output.WriteLine($"{file}: worst edge disagreement {worst:0.000} px");
        Assert.True(failures.Count == 0, $"{file}:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// The PDF raster of a square, and a scene raster covering it on a whole-pixel grid of its own; edges are compared in
    /// page coordinates, so the two grids need not coincide.
    /// </summary>
    private static (Raster Pdf, Raster Scene) Pair(Scene page, double centreX, double centreY, double side)
    {
        var pdf = Raster.Render(page, centreX, centreY, side, Dpi);
        double pixelsPerDmm = Dpi / 254.0;
        int left = (int)Math.Floor(pdf.OriginX * pixelsPerDmm), top = (int)Math.Floor(pdf.OriginY * pixelsPerDmm);
        var image = SceneRasterizer.Rasterize(page, Dpi, 1.0, new PixelRegion(left, top, pdf.Width + 1, pdf.Height + 1));
        return (pdf, Raster.FromImage(image, left / pixelsPerDmm, top / pixelsPerDmm, pixelsPerDmm));
    }

    private static void Compare(string what, Raster pdf, Raster scene, double x, double y, double dx, double dy, double length, List<string> failures, ref double worst)
    {
        var a = Crossings(pdf, x, y, dx, dy, length);
        var b = Crossings(scene, x, y, dx, dy, length);
        if (a.Count != b.Count)
        {
            failures.Add($"{what}: {a.Count} edges in the PDF raster, {b.Count} in the scene raster");
            return;
        }

        for (int i = 0; i < a.Count; i++)
        {
            double difference = Math.Abs(a[i] - b[i]);
            worst = Math.Max(worst, difference);
            if (difference > Tolerance)
            {
                failures.Add($"{what}: edge {i} at {a[i]:0.000} px in the PDF raster, {b[i]:0.000} px in the scene raster");
            }
        }
    }

    /// <summary>Distances in device pixels along the profile at which ink coverage passes 50 percent.</summary>
    private static List<double> Crossings(Raster raster, double x, double y, double dx, double dy, double length)
    {
        var crossings = new List<double>();
        double stepDmm = Step / raster.PixelsPerDmm;
        double previous = raster.Ink(x, y) - 0.5;
        for (double t = stepDmm; t <= length; t += stepDmm)
        {
            double current = raster.Ink(x + (t * dx), y + (t * dy)) - 0.5;
            if (Math.Sign(current) != Math.Sign(previous) && current != previous)
            {
                double at = t - (stepDmm * current / (current - previous));
                crossings.Add(at * raster.PixelsPerDmm);
            }

            previous = current;
        }

        return crossings;
    }
}
