using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 55 section 3 item 1: the stage record says what each located bull rested on. Entry 52 section 3
    /// measured what that is worth: leaving one edge point out moves a bull of 900 points by 0.0002 in and a bull of 14 points by
    /// 0.0032 in, two thirds of the whole gate, and the count is known before anybody looks at the answer. The rays that sat within a
    /// tenth of the crossing threshold are counted beside it, because that test, rather than the fit's rejection, is what decides how
    /// many points a bull has. Nothing about what is located changes.
    /// </summary>
    [Fact]
    public void TheStageRecordSaysHowManyEdgePointsEachBullRestedOnAndHowManyRaysWereMarginal()
    {
        var (image, definition) = Render.Value;
        double dmmPerPixel = 254 / Dpi;
        var mapping = new HomographyMapping(new Homography([dmmPerPixel, 0, 0.5 * dmmPerPixel, 0, dmmPerPixel, 0.5 * dmmPerPixel, 0, 0, 1]));
        var trace = new TraceRecorder();

        var bulls = SheetMeasurer.LocateBulls(image, definition, mapping, new MeasureOptions(), trace);

        var located = bulls.Where(b => b.Recovered is not null).ToList();
        Assert.NotEmpty(located);
        Assert.All(located, b => Assert.True(b.EdgePoints is > 0, $"bull {b.Name} records no edge point count"));
        Assert.All(located, b => Assert.NotNull(b.NearThresholdRays));

        var record = trace.Records.Single(r => r.Stage == "P0.bulls");
        foreach (var bull in located)
        {
            Assert.Equal(bull.EdgePoints!.Value, record.Metrics.Single(m => m.Name == $"edgePoints[{bull.Name}]").Value);
            Assert.Equal(bull.NearThresholdRays!.Value, record.Metrics.Single(m => m.Name == $"raysNearThreshold[{bull.Name}]").Value);
        }

        Assert.Equal(located.Min(b => b.EdgePoints!.Value), record.Metrics.Single(m => m.Name == "fewestEdgePoints").Value);
        Assert.Equal(located.Sum(b => b.NearThresholdRays!.Value), record.Metrics.Single(m => m.Name == "raysNearThreshold").Value);
        Assert.Contains(record.Details, d => d.StartsWith("edge points per located bull:", StringComparison.Ordinal));
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
