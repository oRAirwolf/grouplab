using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// Error report 52 (Discord report 53), nightly 235 on Android: a photograph of the 2 MOA sheet, nine .30 caliber holes, read as sixteen. The
/// printed center dot of seven bulls, 0.197 in across, was found as a hole about 0.14 in across, at most 0.013 in off the bull's center, and the
/// nine real holes, twice as wide, were then each flagged as about 3.3 holes' area. In that photograph the dots read darker than the rest of
/// the print the residual is scaled by. These synthetic sheets darken the dots the same way: a dark mark lying wholly inside a printed dot of
/// a hole's size is the dot, while a hole beside it, a hole across it and a light hole inside it are still holes.
/// </summary>
public class PrintedDotTests
{
    private const double Dpi = 150;

    private static (GrayImage Observed, IPageMapping Truth, GrayImage Render, List<PointD> Holes, List<PointD> Dots) Sheet(bool darkDots, bool lightHoleInADot = false)
    {
        var definition = BuiltIns.Load("GL-CF9-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var holes = new List<SyntheticHole>();
        var centres = new List<PointD>();
        var dots = new List<PointD>();
        for (int k = 0; k < definition.Bulls.Count; k++)
        {
            var bull = definition.Bulls[k];
            dots.Add(new PointD(bull.X, bull.Y));
            // A .30 caliber hole 0.4 to 0.6 in from the bull's center, as on the reported sheet, and on bull 7 one straight across the dot.
            double angle = k * 0.7, reach = k == 6 ? 0 : (0.4 + (0.025 * k)) * 254;
            double x = bull.X + (reach * Math.Cos(angle)), y = bull.Y + (reach * Math.Sin(angle));
            centres.Add(new PointD(x, y));
            holes.Add(new SyntheticHole(x, y, 0.13 * 254, 0.035 * 254, 34, 20, 0.014 * 254, [0.3, 0.2, 0.1, 0.1], [0, 1, 2, k]));
        }

        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], new Random(5212));
        if (darkDots || lightHoleInADot)
        {
            var pixels = (byte[])observed.Pixels.Clone();
            for (int k = 0; k < dots.Count; k++)
            {
                if (k == 6)
                {
                    continue;
                }

                var at = truth.ToImage(dots[k]);
                double radius = 25 * Dpi / 254;
                for (int y = (int)(at.Y - radius - 2); y <= at.Y + radius + 2; y++)
                {
                    for (int x = (int)(at.X - radius - 2); x <= at.X + radius + 2; x++)
                    {
                        double r = Math.Sqrt(((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y)));
                        int i = (y * observed.Width) + x;
                        if (darkDots && r <= radius - 1)
                        {
                            // Black print reads V 65 on these sheets; the reported dots read far darker than the print around them.
                            pixels[i] = 8;
                        }

                        if (lightHoleInADot && k == 0 && r <= radius * 0.8)
                        {
                            // A small hole through the dot onto a flatbed's white lid, as light as the paper.
                            pixels[i] = 235;
                        }
                    }
                }
            }

            observed = new GrayImage(observed.Width, observed.Height, pixels);
        }

        return (observed, truth, render, centres, dots);
    }

    private static double Inches(IPageMapping truth, double x, double y, PointD page)
    {
        var p = truth.ToPage(new PointD(x, y));
        return Math.Sqrt(Math.Pow(p.X - page.X, 2) + Math.Pow(p.Y - page.Y, 2)) / 254;
    }

    [Fact]
    public void ADotReadingDarkerThanThePrintIsNotAHoleAndTheHolesKeepTheirSize()
    {
        var (observed, truth, render, holes, dots) = Sheet(darkDots: true);
        var result = RenderDifferenceHoleDetector.Detect(observed, BuiltIns.Load("GL-CF9-LTR.gltd.json"), 0, truth, Dpi, new OpenCvSharpBackend(), pageRender: render);

        foreach (var hole in holes)
        {
            Assert.Contains(result.Holes, h => Inches(truth, h.X, h.Y, hole) <= 0.05);
        }

        // Nothing sits on a dot apart from bull 7's hole straight across it.
        for (int k = 0; k < dots.Count; k++)
        {
            if (k != 6)
            {
                Assert.DoesNotContain(result.Holes, h => Inches(truth, h.X, h.Y, dots[k]) <= 0.1);
            }
        }

        Assert.Equal(holes.Count, result.Holes.Count);
        Assert.DoesNotContain(result.Holes, h => h.Oversized);
        Assert.Contains(result.Rejected, r => r.Reason == RenderDifferenceHoleDetector.PrintedDot);
    }

    [Fact]
    public void ALightHoleInsideADotIsStillAHole()
    {
        var (observed, truth, render, _, dots) = Sheet(darkDots: false, lightHoleInADot: true);
        var result = RenderDifferenceHoleDetector.Detect(observed, BuiltIns.Load("GL-CF9-LTR.gltd.json"), 0, truth, Dpi, new OpenCvSharpBackend(), pageRender: render);
        Assert.Contains(result.Holes, h => Inches(truth, h.X, h.Y, dots[0]) <= 0.03);
    }
}
