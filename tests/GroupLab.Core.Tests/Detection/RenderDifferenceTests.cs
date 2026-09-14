using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// Render-and-difference on a synthetic GL-CF25-LTR at 150 DPI, docs/PHASE1-BRIEF.md sections 4.2 and 4.3: a hole on every
/// bull, registered exactly, must come back complete and with nothing else. The holes are typical of the survey's, not
/// drawn from its tails: a faint hole, a thin rim around a core at paper level, is the sweep's question
/// (<c>grouplab holes synthetic</c>), not a unit test's.
/// </summary>
public class RenderDifferenceTests(ITestOutputHelper output)
{
    [Fact]
    public void TheExpectedImageLandsOnTheRenderPixelForPixel()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 100;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var identity = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));

        var expected = ExpectedImage.Render(render, dpi, identity, render.Width, render.Height);

        int differing = render.Pixels.Zip(expected.Pixels).Count(p => Math.Abs(p.First - p.Second) > 1);
        Assert.Equal(0, differing);
    }

    /// <summary>The sign of the backend's phase correlation, which the alignment of render-and-difference depends on.</summary>
    [Fact]
    public void PhaseCorrelationReportsHowFarTheMovedImageLiesFromTheReference()
    {
        const int size = 256;
        byte[] Ring(double cx, double cy)
        {
            var pixels = new byte[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double r = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2));
                    pixels[(y * size) + x] = Math.Abs(r - 60) <= 6 || r <= 12 ? (byte)30 : (byte)240;
                }
            }

            return pixels;
        }

        var (shift, response) = new OpenCvSharpBackend().PhaseCorrelate(new GrayImage(size, size, Ring(128, 128)), new GrayImage(size, size, Ring(131, 126)));

        output.WriteLine($"shift ({shift.X:0.00}, {shift.Y:0.00}), response {response:0.00}");
        Assert.True(Math.Abs(shift.X - 3) < 0.3 && Math.Abs(shift.Y + 2) < 0.3, $"shift ({shift.X}, {shift.Y})");
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 23 section 4: detection runs inside the registered sheet. A sheet photographed on a dark mat, here a
    /// synthetic sheet in a frame 120 px wider on every side with everything beyond the page at V 25, still has every hole found and no
    /// candidate outside the page, because the detector masks its residual to the sheet whatever image it is given.
    /// </summary>
    [Fact]
    public void ADarkSurroundBeyondTheSheetIsNeverACandidate()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 100;
        const int border = 120;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, (0.5 - border) * s, 0, s, (0.5 - border) * s, 0, 0, 1]));
        int width = render.Width + (2 * border), height = render.Height + (2 * border);
        var random = new Random(23);
        var holes = definition.Bulls.Select(b =>
        {
            var at = truth.ToImage(new PointD(b.X + 40, b.Y - 30));
            bool onInk = render.Pixels[(Math.Clamp((int)at.Y - border, 0, render.Height - 1) * render.Width) + Math.Clamp((int)at.X - border, 0, render.Width - 1)] < 128;
            return new SyntheticHole(b.X + 40, b.Y - 30, 0.10 * 254, (onInk ? 0.08 : 0.065) * 254, onInk ? 48 : 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3]);
        }).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, width, height, holes, [], random);
        var inside = RenderDifferenceHoleDetector.SheetMask(definition.Page.Width, definition.Page.Height, truth, width, height);
        for (int i = 0; i < observed.Pixels.Length; i++)
        {
            if (!inside[i])
            {
                observed.Pixels[i] = 25;
            }
        }

        Assert.False(inside[0]);
        Assert.True(inside[((border + 5) * width) + border + 5]);
        Assert.False(inside[((border + 5) * width) + border - 3]);

        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), pageRender: render);

        var truthImage = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        int found = truthImage.Count(t => result.Holes.Any(d => Math.Sqrt(Math.Pow(d.X - t.X, 2) + Math.Pow(d.Y - t.Y, 2)) <= 0.15 * dpi));
        output.WriteLine($"{found} of {holes.Count} found, {result.Holes.Count} detections; rejected: {string.Join("; ", result.Rejected.GroupBy(r => r.Reason.Split(',')[0]).Select(g => $"{g.Key} {g.Count()}"))}");
        Assert.Equal(holes.Count, found);
        Assert.Equal(holes.Count, result.Holes.Count);
        Assert.All(result.Rejected, r => Assert.True(inside[((int)r.Y * width) + (int)r.X], $"a candidate at ({r.X:0}, {r.Y:0}) outside the sheet"));
    }

    [Fact]
    public void AHoleOnEveryBullIsFoundAndNothingElse()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(20260914);
        var holes = definition.Bulls.Select(b =>
        {
            double x = b.X + (60 * SyntheticSurface.Gaussian(random)), y = b.Y + (60 * SyntheticSurface.Gaussian(random));
            var at = truth.ToImage(new PointD(x, y));
            bool onInk = render.Pixels[(Math.Clamp((int)at.Y, 0, render.Height - 1) * render.Width) + Math.Clamp((int)at.X, 0, render.Width - 1)] < 128;
            return new SyntheticHole(x, y, 0.10 * 254, (onInk ? 0.08 : 0.065) * 254, onInk ? 48 : 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3]);
        }).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);

        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), pageRender: render);

        int found = holes.Count(h =>
        {
            var at = truth.ToImage(new PointD(h.X, h.Y));
            return result.Holes.Any(d => Math.Sqrt(Math.Pow(d.X - at.X, 2) + Math.Pow(d.Y - at.Y, 2)) <= 0.15 * dpi);
        });
        int stray = result.Holes.Count(d => !holes.Any(h =>
        {
            var at = truth.ToImage(new PointD(h.X, h.Y));
            return Math.Sqrt(Math.Pow(d.X - at.X, 2) + Math.Pow(d.Y - at.Y, 2)) <= 0.15 * dpi;
        }));
        output.WriteLine($"{found} of {holes.Count} found, {stray} strays, ink fraction {result.InkFraction:0.000}; rejected: {string.Join("; ", result.Rejected.GroupBy(r => r.Reason.Split(',')[0]).Select(g => $"{g.Key} {g.Count()}"))}");
        foreach (var missed in holes.Where(h =>
        {
            var at = truth.ToImage(new PointD(h.X, h.Y));
            return !result.Holes.Any(d => Math.Sqrt(Math.Pow(d.X - at.X, 2) + Math.Pow(d.Y - at.Y, 2)) <= 0.15 * dpi);
        }))
        {
            output.WriteLine($"missed at ({missed.X:0}, {missed.Y:0}) dmm: rim radius {missed.RimRadius / 254:0.000} in, rim width {missed.RimWidth / 254:0.000} in, rim V {missed.RimV:0}, core V {missed.CoreV:0}, lobes {string.Join("/", missed.LobeAmplitudes.Select(a => a.ToString("0.00")))}");
        }

        Assert.Equal(holes.Count, found);
        Assert.Equal(0, stray);
    }
}
