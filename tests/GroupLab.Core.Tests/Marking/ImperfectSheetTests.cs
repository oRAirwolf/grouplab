using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// What happens when the sheet is not perfect, NOTES-FROM-PLANNING.md entry 115 section 4: a scan turned any way round, and the messages for
/// a sheet that cannot be read, each driven by an image that provokes it. Every message says what to do next rather than what failed.
/// </summary>
public class ImperfectSheetTests
{
    private const double Dpi = 300;

    private static readonly OpenCvSharpBackend Backend = new();

    private static ImageMetadata Scan(GrayImage image, double dpi = Dpi) => new("PNG", image.Width, image.Height, dpi, dpi, null, null, null, null, null);

    /// <summary>A sheet with a hole on every scoring bull, rendered at <paramref name="dpi"/> and optionally shrunk as a printer would.</summary>
    private static (GrayImage Image, GroupLab.Core.Gltd.Model.TargetDefinition Definition, IReadOnlyList<PointD> Holes) Sheet(double dpi = Dpi, double printScale = 1, string file = "GL-CF25-LTR.gltd.json")
    {
        var definition = BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        var random = new Random(115);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-40, 41), b.Y + random.Next(-40, 41), onInk: false, HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / dpi / printScale;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, dpi, truth, (int)Math.Round(render.Width * printScale), (int)Math.Round(render.Height * printScale), holes, [], random);
        return (image, definition, [.. holes.Select(h => new PointD(h.X, h.Y))]);
    }

    private static GrayImage Turn(GrayImage image, int quarters)
    {
        var turned = image;
        for (int q = 0; q < quarters; q++)
        {
            var next = new byte[turned.Width * turned.Height];
            int width = turned.Height, height = turned.Width;
            for (int y = 0; y < turned.Height; y++)
            {
                for (int x = 0; x < turned.Width; x++)
                {
                    // A quarter turn clockwise: the left column becomes the top row.
                    next[(x * width) + (width - 1 - y)] = turned.Pixels[(y * turned.Width) + x];
                }
            }

            turned = new GrayImage(width, height, next);
        }

        return turned;
    }

    /// <summary>
    /// Entry 115 section 4: the markers carry the page's orientation, so a scan turned by any quarter reads as the same sheet, with every hole
    /// in the same place on the page, to well inside the 0.15 in the gate matches holes by.
    /// </summary>
    [Fact]
    public void AScanTurnedAnyWayRoundGivesTheSameHolesOnThePage()
    {
        var (image, definition, holes) = Sheet();
        var upright = AutomaticMarking.Run(image, image, Scan(image), definition, Backend);
        Assert.Null(upright.Failure);
        var expected = Page(upright);
        Assert.Equal(holes.Count, expected.Count);

        foreach (int quarters in new[] { 1, 2, 3 })
        {
            var turned = Turn(image, quarters);
            var result = AutomaticMarking.Run(turned, turned, Scan(turned), definition, Backend);
            Assert.Null(result.Failure);
            var found = Page(result);
            Assert.Equal(expected.Count, found.Count);
            foreach (var hole in expected)
            {
                double nearest = found.Min(f => Math.Sqrt(Math.Pow(f.X - hole.X, 2) + Math.Pow(f.Y - hole.Y, 2))) / 254;
                Assert.True(nearest < 0.02, $"turned {quarters * 90} degrees: a hole moved {nearest:0.000} in on the page");
            }
        }
    }

    private static List<PointD> Page(AutomaticResult result) =>
        [.. result.Detections.Select(d => result.Scale!.Mapping.ToPage(d.Image))];

    /// <summary>
    /// Entry 115 section 4: a scan with too few pixels says so, with the figure, and what to do about it. The detector copes further down than
    /// one would guess, which the measurement in DetectionAdvice records, so the image this is driven with is one that really does fail.
    /// </summary>
    [Fact]
    public void AScanWithTooFewPixelsSaysSoAndWhatToDo()
    {
        var (thin, definition, _) = Sheet(dpi: 60);
        var failed = AutomaticMarking.Run(thin, thin, Scan(thin, 60), definition, Backend);
        Assert.NotNull(failed.Failure);
        string advice = DetectionAdvice.Failure(failed.Measurement, Scan(thin, 60), definition)!;
        Assert.Contains("pixels to the inch", advice, StringComparison.Ordinal);
        Assert.Contains("Scan it at 300 dpi", advice, StringComparison.Ordinal);

        // At 96 dpi it still reads the sheet, so nothing is said about the resolution.
        var (coarse, _, _) = Sheet(dpi: 96);
        var read = AutomaticMarking.Run(coarse, coarse, Scan(coarse, 96), definition, Backend);
        Assert.Null(read.Failure);
        Assert.Null(DetectionAdvice.Failure(read.Measurement, Scan(coarse, 96), definition));
    }

    /// <summary>
    /// Entry 115 section 4: a sheet read as another definition used to give numbers with nothing said. It registers, because the markers are
    /// the same family, and then measures the wrong bulls. Each of the four sheets this one can be mistaken for is now doubted out loud, and
    /// the sheet read as itself is not.
    /// </summary>
    [Fact]
    public void AnImageReadAsAnotherSheetIsDoubtedOutLoud()
    {
        var (image, own, _) = Sheet();
        Assert.Null(DetectionAdvice.Suspect(AutomaticMarking.Run(image, image, Scan(image), own, Backend).Measurement, own));

        foreach (string file in new[] { "GL-RF36-LTR.gltd.json", "GL-CF30-LTR.gltd.json", "GL-ZERO-MOA-100Y.gltd.json", "GL-CF25-A4.gltd.json" })
        {
            var other = BuiltIns.Load(file);
            var result = AutomaticMarking.Run(image, image, Scan(image), other, Backend);
            string? doubt = DetectionAdvice.Suspect(result.Measurement, other);
            Assert.True(doubt is not null, $"{own.Name} read as {other.Name} said nothing");
            Assert.StartsWith($"This may not be {other.Name}: ", doubt, StringComparison.Ordinal);
            Assert.EndsWith("the figures mean nothing if it is another sheet.", doubt, StringComparison.Ordinal);
        }

        // The sheet's own codes are its word for what it is, and that is said before anything is analysed.
        string codes = DetectionAdvice.WrongSheet(own.Name, BuiltIns.Load("GL-RF36-LTR.gltd.json"));
        Assert.Contains($"codes say it is {own.Name}", codes, StringComparison.Ordinal);
        Assert.Contains("nothing was analysed", codes, StringComparison.Ordinal);
    }

    /// <summary>Entry 115 section 4: a sheet its printer shrank is named as such, with the figure, and its measurements are corrected.</summary>
    [Fact]
    public void ASheetPrintedSmallIsNamedWithItsFigure()
    {
        var (image, definition, holes) = Sheet(printScale: 0.97);
        var result = AutomaticMarking.Run(image, image, Scan(image), definition, Backend);
        Assert.Null(result.Failure);
        string said = DetectionAdvice.PrintScale(result.Measurement)!;
        Assert.Contains("97.0 percent of its intended size", said, StringComparison.Ordinal);
        // Entry 161 section 6: nothing is corrected, so the sentence says which way every size reads and by how much. At 97 percent a sheet
        // inch is 0.97 of a real one, so a size reads 1 / 0.97 = 3.1 percent large.
        Assert.Contains("reads 3.1 percent large", said, StringComparison.Ordinal);
        Assert.DoesNotContain("corrected", said, StringComparison.Ordinal);

        // Corrected: every hole is still where it was put on the page, whatever the printer did to the paper.
        var found = Page(result);
        foreach (var hole in holes)
        {
            double nearest = found.Min(f => Math.Sqrt(Math.Pow(f.X - hole.X, 2) + Math.Pow(f.Y - hole.Y, 2))) / 254;
            Assert.True(nearest < 0.02, $"a hole is {nearest:0.000} in from where it was put");
        }

        // A sheet printed at its own size says nothing about scale.
        var (full, _, _) = Sheet();
        Assert.Null(DetectionAdvice.PrintScale(AutomaticMarking.Run(full, full, Scan(full), definition, Backend).Measurement));
    }
}
