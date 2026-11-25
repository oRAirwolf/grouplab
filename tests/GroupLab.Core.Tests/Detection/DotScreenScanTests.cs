using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// The robustness check of report 60's fix (Alan's queue of 2026-11-24, part 4), kept as a test: a laser printer's screen as a scanner sees
/// it, the dots drawn at 45 degrees and each scan pixel the share of it they cover, for GroupLab's own tints (about 35 to 42 percent on a
/// black and white printer) and darker, at a fine and a coarse screen. With the scan check every hole is found and nothing else; without it
/// the fine screen at 300 dpi made dozens of false holes. A plain black print with paper grain never reads as a dot pattern. The sweep this
/// came from, over 150, 300 and 600 dpi and 30 to 60 percent, is in PHASE1-RESULTS.md.
/// </summary>
public class DotScreenScanTests
{
    private const double Dpi = 300;
    private const string Target = "GL-CF9-LTR-E.gltd.json";

    private static (GrayImage Value, IPageMapping Truth, GrayImage Render, List<PointD> Holes) Sheet(double? coverage, double lpi, double grain = 0)
    {
        var definition = BuiltIns.Load(Target);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, [], [], new Random(6024));
        var pixels = (byte[])observed.Pixels.Clone();
        var bulls = definition.Bulls.Where(b => b.Scoring).Select(b => truth.ToImage(new PointD(b.X, b.Y))).ToList();
        int width = observed.Width;
        var noise = new Random(17);
        double period = Dpi / lpi;
        for (int y = 0; y < observed.Height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width) + x;
                if (grain > 0)
                {
                    pixels[i] = (byte)Math.Clamp(pixels[i] + (grain * (noise.NextDouble() + noise.NextDouble() + noise.NextDouble() - 1.5)), 0, 255);
                }

                if (coverage is not { } dark || render.Pixels[i] > 20 || !bulls.Any(b => Near(b, x, y, Dpi) && !Near(b, x, y, 0.15 * Dpi)))
                {
                    continue;
                }

                double radius = Math.Sqrt(dark / Math.PI) * period;
                int covered = 0;
                for (int sy = 0; sy < 5; sy++)
                {
                    for (int sx = 0; sx < 5; sx++)
                    {
                        double px = x + ((sx + 0.5) / 5), py = y + ((sy + 0.5) / 5);
                        double du = ((((px + py) / Math.Sqrt(2)) / period) - Math.Round(((px + py) / Math.Sqrt(2)) / period)) * period;
                        double dv = ((((px - py) / Math.Sqrt(2)) / period) - Math.Round(((px - py) / Math.Sqrt(2)) / period)) * period;
                        covered += (du * du) + (dv * dv) < radius * radius ? 1 : 0;
                    }
                }

                pixels[i] = (byte)Math.Clamp(236 - (Math.Min(1, covered / 25.0) * (236 - 45)) + (4 * (noise.NextDouble() - 0.5)), 0, 255);
            }
        }

        // A .22 hole on each bull's ring, showing the scanner's white lid through a torn grey edge.
        var holes = new List<PointD>();
        for (int k = 0; k < bulls.Count; k++)
        {
            double angle = 0.4 + (k * 0.9), reach = (0.55 + (0.03 * k)) * Dpi;
            var at = new PointD(bulls[k].X + (reach * Math.Cos(angle)), bulls[k].Y + (reach * Math.Sin(angle)));
            holes.Add(truth.ToPage(at));
            for (int y = (int)at.Y - 42; y <= at.Y + 42; y++)
            {
                for (int x = (int)at.X - 42; x <= at.X + 42; x++)
                {
                    double r = Math.Sqrt(((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y))) / Dpi;
                    double edge = 0.1 + (0.012 * Math.Sin(7 * Math.Atan2(y - at.Y, x - at.X)));
                    if (r <= edge)
                    {
                        pixels[(y * width) + x] = r <= edge - 0.025 ? (byte)238 : (byte)150;
                    }
                }
            }
        }

        return (new GrayImage(observed.Width, observed.Height, pixels), truth, render, holes);
    }

    private static bool Near(PointD at, double x, double y, double radius) => ((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y)) <= radius * radius;

    private static RenderDifferenceResult Detect(GrayImage value, IPageMapping truth, GrayImage render, bool scan) =>
        RenderDifferenceHoleDetector.Detect(value, BuiltIns.Load(Target), 0, truth, Dpi, new OpenCvSharpBackend(),
            new RenderDifferenceOptions(Scan: scan, CalibreInches: 0.224), render, value);

    private static int Found(RenderDifferenceResult result, IPageMapping truth, List<PointD> holes) =>
        holes.Count(hole => result.Holes.Any(h =>
        {
            var p = truth.ToPage(new PointD(h.X, h.Y));
            return Math.Sqrt(Math.Pow(p.X - hole.X, 2) + Math.Pow(p.Y - hole.Y, 2)) / 254 <= 0.04;
        }));

    [Theory]
    [InlineData(0.4, 141)]
    [InlineData(0.6, 141)]
    [InlineData(0.4, 85)]
    public void EveryHoleInALaserScreenIsFoundAndNothingElse(double coverage, double lpi)
    {
        var (value, truth, render, holes) = Sheet(coverage, lpi);
        var result = Detect(value, truth, render, scan: true);
        Assert.True(result.Halftone is { DotPattern: true }, $"texture {result.Halftone?.Texture}");
        Assert.Equal(holes.Count, Found(result, truth, holes));
        Assert.Equal(holes.Count, result.Holes.Count);
    }

    [Fact]
    public void WithoutTheScanCheckTheFineScreenMadeFalseHoles()
    {
        var (value, truth, render, holes) = Sheet(0.4, 141);
        Assert.True(Detect(value, truth, render, scan: false).Holes.Count > holes.Count + 10);
    }

    [Fact]
    public void APlainBlackPrintWithPaperGrainIsNotADotPattern()
    {
        var (value, truth, render, holes) = Sheet(null, 106, grain: 20);
        var result = Detect(value, truth, render, scan: true);
        Assert.False(result.Halftone!.DotPattern, $"texture {result.Halftone.Texture}");
        Assert.True(result.Halftone.Texture < RenderDifferenceHoleDetector.HalftoneTextureAtLeast / 1.5, $"texture {result.Halftone.Texture}");
        Assert.Equal(holes.Count, Found(result, truth, holes));
    }
}
