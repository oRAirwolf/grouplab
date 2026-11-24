using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// Error report 60 (Discord, nightly 243 on a Mac): a 3x3 2 MOA E bull sheet with blue bulls, printed on a black and white laser printer and
/// scanned flat in color at 150 dpi. The printer made the blue a grey of dots, the scan turned the dots into a speckle and rainbow bands, and
/// hole finding marked 55 holes for 9 shots. These synthetic sheets draw the E bull's ring as a speckle of dots under broad bands, with .22 holes
/// through it showing the white lid: on a scan, printed paper now shows a hole only where it reaches near the paper's white, and a sheet
/// printed in solid black is read exactly as before.
/// </summary>
public class DotPatternScanTests
{
    private const double Dpi = 150;
    private const string Target = "GL-CF9-LTR-E.gltd.json";

    private static (GrayImage Value, GrayImage Luminance, IPageMapping Truth, GrayImage Render, List<PointD> Holes) Sheet(bool dots)
    {
        var definition = BuiltIns.Load(Target);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, [], [], new Random(6024));
        var pixels = (byte[])observed.Pixels.Clone();
        var colour = new byte[pixels.Length];
        var bulls = definition.Bulls.Select(b => truth.ToImage(new PointD(b.X, b.Y))).ToList();
        int width = observed.Width;

        if (dots)
        {
            // The ring, the print within an inch of a bull's center and outside its center dot, as a laser printer's grey reaches a 150 dpi
            // scan: each pixel dark or paper, at random, as the dots fall across it, dark from two fifths to four fifths of the time
            // in broad bands.
            var speckle = new Random(60);
            for (int y = 0; y < observed.Height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width) + x;
                    if (render.Pixels[i] > 20 || !bulls.Any(b => Near(b, x, y, 1.0 * Dpi) && !Near(b, x, y, 0.15 * Dpi)))
                    {
                        continue;
                    }

                    double dark = 0.6 + (0.2 * Math.Sin(((x * 0.6) + y) / 23.0));
                    pixels[i] = speckle.NextDouble() < dark ? (byte)45 : (byte)236;

                    // The rainbow the scan makes of the dots: bands where the brightest of red, green and blue stands well above the grey.
                    colour[i] = (byte)Math.Round(34 * Math.Max(0, Math.Sin((x - (0.8 * y)) / 19.0)));
                }
            }
        }

        // A .22 hole on each bull's ring, about 0.2 in across, showing the scanner's white lid through a torn grey edge.
        var holes = new List<PointD>();
        for (int k = 0; k < bulls.Count; k++)
        {
            double angle = 0.4 + (k * 0.9), reach = (0.55 + (0.03 * k)) * Dpi;
            var at = new PointD(bulls[k].X + (reach * Math.Cos(angle)), bulls[k].Y + (reach * Math.Sin(angle)));
            holes.Add(truth.ToPage(at));
            for (int y = (int)at.Y - 20; y <= at.Y + 20; y++)
            {
                for (int x = (int)at.X - 20; x <= at.X + 20; x++)
                {
                    double r = Math.Sqrt(((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y))) / Dpi;
                    double edge = 0.1 + (0.012 * Math.Sin(7 * Math.Atan2(y - at.Y, x - at.X)));
                    if (r <= edge)
                    {
                        pixels[(y * width) + x] = r <= edge - 0.025 ? (byte)238 : (byte)150;
                        colour[(y * width) + x] = 0;
                    }
                }
            }
        }

        var value = pixels.Select((p, i) => (byte)Math.Min(255, p + colour[i])).ToArray();
        return (new GrayImage(observed.Width, observed.Height, value), new GrayImage(observed.Width, observed.Height, pixels), truth, render, holes);
    }

    private static bool Near(PointD at, double x, double y, double radius) => ((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y)) <= radius * radius;

    private static double Inches(IPageMapping truth, double x, double y, PointD page)
    {
        var p = truth.ToPage(new PointD(x, y));
        return Math.Sqrt(Math.Pow(p.X - page.X, 2) + Math.Pow(p.Y - page.Y, 2)) / 254;
    }

    private static RenderDifferenceResult Detect(GrayImage value, GrayImage luminance, IPageMapping truth, GrayImage render, bool scan, double? calibre = null) =>
        RenderDifferenceHoleDetector.Detect(value, BuiltIns.Load(Target), 0, truth, Dpi, new OpenCvSharpBackend(),
            new RenderDifferenceOptions(Scan: scan, CalibreInches: calibre), render, luminance);

    [Theory]
    [InlineData(null)]
    [InlineData(0.212)]
    public void OnAScanPrintedAsDotsOnlyTheHolesAreFound(double? calibre)
    {
        var (value, luminance, truth, render, holes) = Sheet(dots: true);
        var result = Detect(value, luminance, truth, render, scan: true, calibre);

        Assert.True(result.Halftone is { DotPattern: true }, $"texture {result.Halftone?.Texture}");
        foreach (var hole in holes)
        {
            Assert.Contains(result.Holes, h => Inches(truth, h.X, h.Y, hole) <= 0.04);
        }

        Assert.Equal(holes.Count, result.Holes.Count);
    }

    [Fact]
    public void WithoutTheScanCheckTheDotsAreTakenForHoles()
    {
        // What nightly 243 did with the same picture, and what a photograph of it is still judged by.
        var (value, luminance, truth, render, holes) = Sheet(dots: true);
        Assert.True(Detect(value, luminance, truth, render, scan: false).Holes.Count > holes.Count);
    }

    [Fact]
    public void ASheetPrintedInSolidBlackIsReadAsBefore()
    {
        var (value, luminance, truth, render, _) = Sheet(dots: false);
        var asScan = Detect(value, luminance, truth, render, scan: true);
        var asBefore = Detect(value, luminance, truth, render, scan: false);

        Assert.False(asScan.Halftone!.DotPattern, $"texture {asScan.Halftone.Texture}");
        Assert.Equal(asBefore.Holes.Select(h => (h.X, h.Y)), asScan.Holes.Select(h => (h.X, h.Y)));
    }
}
