using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// Conformance test 42 of TARGET-SCHEMA.md section 10: the ink extent of every annulus in the rendered output matches
/// the difference of its two declared diameters to within half a device pixel, at 300 and at 600 DPI. The PDF is
/// rasterised by PDFium, an independent renderer, and ink is integrated along radial profiles rather than read off a
/// 50 percent threshold, because a 0.6 mm annulus is barely one pixel wide at 300 DPI and has no clean edge to find.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class AnnulusExtentTests(ITestOutputHelper output)
{
    private const int Directions = 16;
    private const double Step = 0.05;

    public static TheoryData<string, int> Cases()
    {
        var data = new TheoryData<string, int>();
        foreach (string file in BuiltIns.Files)
        {
            data.Add(file, 300);
            data.Add(file, 600);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Test42AnnulusInkExtentsMatchTheDeclaredDiameters(string file, int dpi)
    {
        var d = BuiltIns.Load(file);
        var result = TargetRenderer.Render(d);
        var bands = result.Pages[0].Items.OfType<DiscBand>().ToList();
        int[] sample = [.. new[] { 0, d.Bulls.Count(b => b.Scoring) - 1, d.Bulls.Count - 1 }.Distinct()];
        var failures = new List<string>();
        double worst = 0;

        foreach (int index in sample)
        {
            var bull = d.Bulls[index];
            var own = bands.Where(b => b.CentreX == 2L * bull.X && b.CentreY == 2L * bull.Y).OrderByDescending(b => b.OuterRadius).ToList();
            double outermost = own[0].OuterRadius / 2.0;
            var raster = Raster.Render(result.Pages[0], bull.X, bull.Y, (2 * outermost) + 40, dpi);
            double pixel = 1 / raster.PixelsPerDmm;

            // Profiles run from the declared centre. The raster's own ink centroid is reported beside any failure, so an
            // extent error can be told apart from the rasteriser placing the crop off by a pixel.
            var (centreX, centreY) = InkCentroid(raster, bull.X, bull.Y, outermost + 5, pixel);
            double offset = Math.Sqrt(Math.Pow(centreX - bull.X, 2) + Math.Pow(centreY - bull.Y, 2)) * raster.PixelsPerDmm;

            for (int k = 0; k < own.Count; k++)
            {
                double outer = own[k].OuterRadius / 2.0, inner = own[k].InnerRadius / 2.0;
                double gapOut = k == 0 ? double.PositiveInfinity : (own[k - 1].InnerRadius / 2.0) - outer;
                double gapIn = k == own.Count - 1 ? (inner > 0 ? inner : double.PositiveInfinity) : inner - (own[k + 1].OuterRadius / 2.0);
                double margin = Math.Min(pixel, Math.Min(gapOut, gapIn) / 2);

                // An annulus is measured along each ray, which crosses its band once. A solid disc is measured along a
                // whole diameter, so a centre error of a fraction of a pixel cancels rather than counting against it.
                // On a zeroing sheet the grid's axes cross the aiming mark, so rays along them measure grid ink too.
                bool gridThroughBull = d.Grids?.Any(g => g.CentreX == bull.X && g.CentreY == bull.Y) ?? false;
                for (int n = 0; n < Directions; n++)
                {
                    if (gridThroughBull && n % (Directions / 4) == 0)
                    {
                        continue;
                    }

                    double angle = n * 2 * Math.PI / Directions;
                    double cos = Math.Cos(angle), sin = Math.Sin(angle);
                    double start = inner > 0 ? inner - margin : -(outer + margin), end = outer + margin;
                    double measured = 0;
                    for (double r = start + (Step * pixel / 2); r < end; r += Step * pixel)
                    {
                        measured += raster.Ink(bull.X + (r * cos), bull.Y + (r * sin)) * Step;
                    }

                    double declared = (inner > 0 ? outer - inner : 2 * outer) * raster.PixelsPerDmm;
                    worst = Math.Max(worst, Math.Abs(measured - declared));
                    if (Math.Abs(measured - declared) > 0.5)
                    {
                        failures.Add($"bull {index} band {own[k].OuterRadius}/{own[k].InnerRadius} at {angle * 180 / Math.PI:0} deg: " +
                            $"{measured:0.000} px against {declared:0.000} px (raster centre {offset:0.00} px from declared)");
                    }
                }
            }
        }

        output.WriteLine($"{file} at {dpi} DPI: worst extent error {worst:0.000} px");
        Assert.True(failures.Count == 0, $"{file} at {dpi} DPI:\n{string.Join("\n", failures)}");
    }

    /// <summary>The ink-weighted centroid of the raster within <paramref name="radius"/> dmm of the expected centre.</summary>
    private static (double X, double Y) InkCentroid(Raster raster, double expectedX, double expectedY, double radius, double pixel)
    {
        double sumX = 0, sumY = 0, sum = 0;
        for (double y = expectedY - radius; y <= expectedY + radius; y += pixel)
        {
            for (double x = expectedX - radius; x <= expectedX + radius; x += pixel)
            {
                if (((x - expectedX) * (x - expectedX)) + ((y - expectedY) * (y - expectedY)) > radius * radius)
                {
                    continue;
                }

                double ink = raster.Ink(x, y);
                sumX += ink * x;
                sumY += ink * y;
                sum += ink;
            }
        }

        return (sumX / sum, sumY / sum);
    }
}
