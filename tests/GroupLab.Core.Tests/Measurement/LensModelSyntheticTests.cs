using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// The photograph path of DESIGN.md section 11 on a synthetic photograph whose distortion is known exactly, so that a
/// photograph gate failure on paper can be attributed to the photograph rather than to the code. GL-CF25-LTR is rendered,
/// then resampled into a 4000 by 3000 frame turned a quarter turn, with keystone perspective and radial distortion of the
/// model's own form at three strengths, the middle one about the size the Phase 0 photographs fitted. Every bull must come
/// back inside conformance test 43's gate.
/// </summary>
public class LensModelSyntheticTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.01, -0.02)]
    [InlineData(0.08, -0.05)]
    public void TheLensModelRecoversEveryBullThroughKnownDistortionAndPerspective(double k1, double k2)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        const int width = 4000, height = 3000;
        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0, s = Math.Max(width, height) / 2.0;
        var backend = new OpenCvSharpBackend();

        // Undistorted normalised frame coordinates to render pixels: the page's top-left, top-right, bottom-right and
        // bottom-left corners placed as a quarter-turned keystone, bottom edge longer than the top.
        PointD[] frame = [new(0.70, -0.62), new(0.66, 0.66), new(-0.72, 0.72), new(-0.66, -0.66)];
        PointD[] pageCorners = [new(-0.5, -0.5), new(render.Width - 0.5, -0.5), new(render.Width - 0.5, render.Height - 0.5), new(-0.5, render.Height - 0.5)];
        var toRender = backend.FindHomography(frame, pageCorners, 1000).Transform;

        var pixels = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double u = (x - cx) / s, v = (y - cy) / s, r2 = (u * u) + (v * v), g = 1 + (k1 * r2) + (k2 * r2 * r2);
                var q = toRender.Apply(new PointD(u * g, v * g));
                pixels[(y * width) + x] = Sample(render, q.X, q.Y);
            }
        }

        var photograph = new GrayImage(width, height, pixels);
        var metadata = new ImageMetadata("synthetic", width, height, null, null, "synthetic", "camera", 1, 2.2, 23);

        var result = SheetMeasurer.Measure(photograph, metadata, definition, new MeasureOptions(), backend);

        Assert.Null(result.Failure);
        var lens = Assert.IsType<RadialHomographyMapping>(result.Registration!.Mapping);
        output.WriteLine($"k1 {k1:+0.000;-0.000} k2 {k2:+0.000;-0.000}: fitted {lens.K1:+0.0000;-0.0000} {lens.K2:+0.0000;-0.0000}, "
            + $"{result.Fiducials!.Matches.Count}/{result.Fiducials.Expected} markers, residual rms {result.Registration.RmsResidual / 254:0.00000} in "
            + $"(homography alone {result.Registration.HomographyRmsResidual / 254:0.00000}), bull mean {result.MeanError / 254:0.00000} in, worst {result.WorstError / 254:0.00000} in at {result.WorstBull?.Name}");
        foreach (var failed in result.Bulls.Where(b => b.Failure is not null))
        {
            output.WriteLine($"  bull {failed.Name} at ({failed.Declared.X / 254:0.00}, {failed.Declared.Y / 254:0.00}) in: {failed.Failure}");
        }

        Assert.All(result.Bulls, b => Assert.Null(b.Failure));
        Assert.True(result.WorstError < SyntheticScanCheck.Gate, $"worst bull {result.WorstError / 254:0.00000} in at {result.WorstBull?.Name}");
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
