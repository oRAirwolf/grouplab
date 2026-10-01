using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// Entry 322 section 2: which marker corners a photograph's lens fit keeps. The 9 and 15 degree pictures of the 2026-09-29 sitting read
/// all 34 markers and the fit left the far column out. A flat sheet seen 15 degrees off square through an ultrawide's distortion is
/// put where every corner was read by a perspective and the lens together, so every corner is kept; a sheet whose far column is lifted
/// off its plane is not, so that column is left out and nothing else is.
/// </summary>
public class LensFitKeepsConsistentTests(ITestOutputHelper output)
{
    private const int Width = 3266, Height = 2449;

    [Theory]
    [InlineData(15, 0.0, 0.0)]
    [InlineData(15, 0.08, -0.05)]
    [InlineData(15, -0.12, 0.02)]
    [InlineData(30, 0.08, -0.05)]
    [InlineData(30, -0.3, 0.06)]
    [InlineData(37, 0.2, -0.1)]
    public void EveryCornerOfAFlatSheetIsKeptThroughPerspectiveAndTheLens(double degrees, double k1, double k2)
    {
        var (image, page) = Corners(degrees, k1, k2, liftDmm: 0);

        var (lens, kept) = Fit(image, page);

        output.WriteLine($"{degrees} degrees, k1 {k1} k2 {k2}: kept {kept.Count(k => k)} of {kept.Length}, fitted k1 {lens.K1:0.0000} k2 {lens.K2:0.0000}");
        Assert.All(kept, Assert.True);
    }

    [Fact]
    public void OneReclassificationWasNotEnoughWhereTheLensBendsHard()
    {
        // The rule before entry 322: a plain homography's inliers, one lens fit, one reclassification. At this strength the homography
        // keeps under half the corners, the lens fit from them reaches the edge only by extrapolating, and a third are left out though
        // nothing is wrong with them; refitting until the kept set stops changing keeps them all.
        var (image, page) = Corners(30, -0.3, 0.06, liftDmm: 0);
        var homography = new OpenCvSharpBackend().FindHomography(image, page, SheetMeasurer.PhotographRansacThreshold);

        int once = SheetMeasurer.FitLens(image, page, homography.Inliers, homography.Transform, Width, Height, rounds: 1).Kept.Count(k => k);
        int settled = SheetMeasurer.FitLens(image, page, homography.Inliers, homography.Transform, Width, Height).Kept.Count(k => k);

        output.WriteLine($"homography {homography.Inliers.Count(k => k)}, one reclassification {once}, settled {settled} of {page.Count}");
        Assert.True(once < page.Count);
        Assert.Equal(page.Count, settled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    public void AColumnLiftedOffThePlaneIsLeftOutAndNothingElse(double degrees)
    {
        // Six millimetres of lift at the far column, about what the sitting's 9 and 15 degree pictures put there.
        var (image, page) = Corners(degrees, 0.01, -0.02, liftDmm: 60);

        var (_, kept) = Fit(image, page);

        for (int i = 0; i < page.Count; i++)
        {
            Assert.True(kept[i] != Lifted(page[i]), $"corner at ({page[i].X / 254:0.00}, {page[i].Y / 254:0.00}) in kept {kept[i]}");
        }
    }

    private static bool Lifted(PointD page) => page.X > 7.5 * 254;

    private static (RadialHomographyMapping Lens, bool[] Kept) Fit(List<PointD> image, List<PointD> page)
    {
        var homography = new OpenCvSharpBackend().FindHomography(image, page, SheetMeasurer.PhotographRansacThreshold);
        return SheetMeasurer.FitLens(image, page, homography.Inliers, homography.Transform, Width, Height);
    }

    /// <summary>
    /// The 34 markers' corners of GL-CF25-LTR-D on a sheet turned <paramref name="degrees"/> about its upright axis, seen by a 13 mm
    /// equivalent lens from about 6 in, distorted by the lens model's own form, read with 0.2 px of noise. Corners right of 7.5 in are
    /// lifted <paramref name="liftDmm"/> off the plane toward the camera.
    /// </summary>
    private static (List<PointD> Image, List<PointD> Page) Corners(double degrees, double k1, double k2, double liftDmm)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR-D.gltd.json");
        double half = definition.Fiducials!.MarkerSize / 2.0;
        double w = definition.Page.Width, h = definition.Page.Height;
        double theta = degrees * Math.PI / 180, focal = 1180, distance = 1500;
        double cx = (Width - 1) / 2.0, cy = (Height - 1) / 2.0, s = Math.Max(Width, Height) / 2.0;
        var random = new Random(322);
        var image = new List<PointD>();
        var page = new List<PointD>();
        foreach (var m in PageRegistration.ExpectedMarkers(definition, 0))
        {
            foreach (var p in (PointD[])[new(m.X - half, m.Y - half), new(m.X + half, m.Y - half), new(m.X + half, m.Y + half), new(m.X - half, m.Y + half)])
            {
                // Page y runs along the image's long side, as in the sitting's pictures.
                double along = p.Y - (h / 2), across = p.X - (w / 2);
                double x3 = along, y3 = across * Math.Cos(theta), z3 = distance + (across * Math.Sin(theta));
                if (Lifted(p))
                {
                    y3 += liftDmm * Math.Sin(theta);
                    z3 -= liftDmm * Math.Cos(theta);
                }

                double u = focal * x3 / z3 / s, v = focal * y3 / z3 / s;

                // The model undistorts by multiplying by 1 + k1 r^2 + k2 r^4 of the distorted radius; invert it by fixed point.
                double du = u, dv = v;
                for (int i = 0; i < 50; i++)
                {
                    double r2 = (du * du) + (dv * dv), g = 1 + (k1 * r2) + (k2 * r2 * r2);
                    (du, dv) = (u / g, v / g);
                }

                image.Add(new PointD(cx + (du * s) + (0.2 * Gaussian(random)), cy + (dv * s) + (0.2 * Gaussian(random))));
                page.Add(p);
            }
        }

        return (image, page);
    }

    private static double Gaussian(Random random) =>
        Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
}
