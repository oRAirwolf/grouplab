using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 15 section 3 step 1: <see cref="EdgeFitBullLocator"/> has no input that throws. A
/// registration that goes wrong at a bull fails that bull with a reason, and the sheet carries on. The Phase 1 real-frame
/// run lost three whole frames to an index exception when a degenerate surface fit put fewer than three samples across an
/// edge (PHASE1-RESULTS.md M1.4). Each case here is a real render of GL-CF25-LTR under a mapping broken one way.
/// </summary>
public class EdgeFitGuardTests
{
    private const double Dpi = 100;

    private static readonly Lazy<(GrayImage Image, TargetDefinition Definition)> Render = new(() =>
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        return (SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi), definition);
    });

    public static TheoryData<string> Breakages => ["none", "scale 1000 times", "scale a millionth", "zero scale", "NaN scale", "infinite scale", "image NaN", "page NaN"];

    [Theory]
    [MemberData(nameof(Breakages))]
    public void ABrokenMappingFailsTheBullWithAReasonAndNeverThrows(string breakage)
    {
        var (image, definition) = Render.Value;
        double dmmPerPixel = 254 / Dpi;
        var planar = new HomographyMapping(new Homography([dmmPerPixel, 0, 0.5 * dmmPerPixel, 0, dmmPerPixel, 0.5 * dmmPerPixel, 0, 0, 1]));
        var mapping = new Broken(planar, breakage);
        var bull = definition.Bulls[0];
        var bands = RingGeometry.Bands(definition, definition.RingSets.First(s => s.Key == bull.RingSet));

        var located = EdgeFitBullLocator.Locate(image, mapping, 0, bull, bands);

        if (breakage == "none")
        {
            Assert.Null(located.Failure);
            Assert.NotNull(located.Recovered);
        }
        else
        {
            Assert.Null(located.Recovered);
            Assert.False(string.IsNullOrWhiteSpace(located.Failure));
        }
    }

    private sealed class Broken(IPageMapping inner, string breakage) : IPageMapping
    {
        public string Model => inner.Model;

        public PointD ToPage(PointD image) => breakage == "page NaN" ? new PointD(double.NaN, double.NaN) : inner.ToPage(image);

        public PointD ToImage(PointD page) => breakage == "image NaN" ? new PointD(double.NaN, double.NaN) : inner.ToImage(page);

        public (double XX, double XY, double YX, double YY) Jacobian(PointD image)
        {
            var j = inner.Jacobian(image);
            double factor = breakage switch
            {
                "scale 1000 times" => 1000,
                "scale a millionth" => 1e-6,
                "zero scale" => 0,
                "NaN scale" => double.NaN,
                "infinite scale" => double.PositiveInfinity,
                _ => 1,
            };
            return (j.XX * factor, j.XY * factor, j.YX * factor, j.YY * factor);
        }
    }
}
