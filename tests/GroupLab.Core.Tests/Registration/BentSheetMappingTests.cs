using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 324 section 1: a margin lifted off the sheet's plane is followed by a smooth correction over the lens fit,
/// and a flat sheet is left exactly as the lens fit reads it.
/// </summary>
public class BentSheetMappingTests
{
    private const double Dpi = 300;

    /// <summary>
    /// GL-CF25-LTR photographed square, its right margin lifted toward the camera: each point right of seven tenths of the width is moved
    /// outward by <paramref name="lift"/> pixels times the square of how far across that last part it is, so the far column of markers reads
    /// about two thirds of the lift off and the column one in about a twentieth: at 30 pixels, about 16 dmm, the size of the far column's
    /// lift on the 9 and 15 degree pictures of entry 322.
    /// </summary>
    private static (GrayImage Image, Func<PointD, PointD> PageToImage) Lifted(double lift)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var flat = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi, from = 0.7 * flat.Width, across = flat.Width - from;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        double Shift(double x) => x <= from ? 0 : lift * Math.Pow((x - from) / across, 2);
        var pixels = new byte[flat.Pixels.Length];
        for (int x = 0; x < flat.Width; x++)
        {
            // The source column each output column shows, by repeated substitution: the shift is small and grows slowly.
            double source = x;
            for (int i = 0; i < 8; i++)
            {
                source = x - Shift(source);
            }

            int x0 = (int)Math.Floor(source);
            double f = source - x0;
            int a = Math.Clamp(x0, 0, flat.Width - 1), b = Math.Clamp(x0 + 1, 0, flat.Width - 1);
            for (int y = 0; y < flat.Height; y++)
            {
                double v = (flat.Pixels[(y * flat.Width) + a] * (1 - f)) + (flat.Pixels[(y * flat.Width) + b] * f);
                pixels[(y * flat.Width) + x] = (byte)Math.Round(v * 225 / 255);
            }
        }

        PointD PageToImage(PointD page)
        {
            var p = truth.ToImage(page);
            return new PointD(p.X + Shift(p.X), p.Y);
        }

        return (new GrayImage(flat.Width, flat.Height, pixels), PageToImage);
    }

    private static ImageMetadata Photo(GrayImage image) => new("JPEG", image.Width, image.Height, null, null, "Test", "Lift", 1, 6.25, 24);

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    [Fact]
    public void ASheetWithALiftedMarginRegistersWithSmallErrorAtEveryBull()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, pageToImage) = Lifted(30);
        var measured = SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.Null(measured.Failure);
        var bent = Assert.IsType<BentSheetMapping>(measured.Registration!.Mapping);
        var bulls = definition.Bulls.Select(b => new PointD(b.X, b.Y)).ToList();
        double worst = bulls.Max(b => Distance(bent.ToPage(pageToImage(b)), b)) / 254;
        double lensWorst = bulls.Max(b => Distance(bent.Lens.ToPage(pageToImage(b)), b)) / 254;
        Assert.True(worst < 0.01, $"the bent sheet is {worst:0.0000} in off at its worst bull, the lens fit alone {lensWorst:0.0000}");
        Assert.True(lensWorst > 3 * worst, $"the lens fit alone is {lensWorst:0.0000} in off at its worst bull, the bent sheet {worst:0.0000}");

        // The page back to the picture agrees with the picture to the page.
        foreach (var b in bulls)
        {
            Assert.True(Distance(bent.ToImage(bent.ToPage(pageToImage(b))), pageToImage(b)) < 0.01);
        }
    }

    /// <summary>A flat sheet's lens fit keeps its corners, and no correction is laid over it: nothing it reads changes.</summary>
    [Fact]
    public void AFlatSheetHasNoCorrection()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, _) = Lifted(0);
        var measured = SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.Null(measured.Failure);
        Assert.IsType<RadialHomographyMapping>(measured.Registration!.Mapping);
    }

    /// <summary>
    /// Corners alone: a flat sheet whose lens fit left a few corners out by noise just past the threshold, and one misread marker far off on
    /// its own, gets no correction, because nothing left out is both well off the lens fit and where its neighbours say it is.
    /// </summary>
    [Fact]
    public void NoiseAndAMisreadMarkerBringNoCorrection()
    {
        var lens = new RadialHomographyMapping(1500, 2000, 2000, -0.04, 0.01, new Homography([1100, 20, 1080, -15, 1100, 1400, 0.01, 0.02, 1]));
        var random = new Random(324);
        var page = new List<PointD>();
        foreach (double x in new[] { 0.5, 2, 3.5, 5, 6.5, 8 })
        {
            foreach (double y in new[] { 1.6, 3.1, 4.6, 6.1, 7.6, 9.1 })
            {
                page.AddRange([new(x * 254, y * 254), new((x * 254) + 40, y * 254), new((x * 254) + 40, (y * 254) + 40), new(x * 254, (y * 254) + 40)]);
            }
        }

        var read = page.Select((p, i) =>
        {
            double off = i is 20 or 57 or 90 ? 3.2 : 0;
            double misread = i / 4 == 14 ? 30 : 0;
            return lens.ToImage(new PointD(p.X + off + misread + ((random.NextDouble() - 0.5) * 0.6), p.Y + ((random.NextDouble() - 0.5) * 0.6)));
        }).ToList();
        var kept = read.Select((q, i) => Distance(lens.ToPage(q), page[i]) <= PageRegistration.RansacThreshold).ToList();
        Assert.Contains(false, kept);
        Assert.Null(BentSheetMapping.Fit(lens, read, page, kept));
    }

    /// <summary>A session measured through a bent sheet reopens with the same mapping, with no image, as every other registration does.</summary>
    [Fact]
    public void ASavedSessionKeepsItsBentSheet()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, _) = Lifted(30);
        var bent = Assert.IsType<BentSheetMapping>(SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend()).Registration!.Mapping);
        var session = new MarkingSession();
        session.Open("sheet.png", 1);
        session.SetScale(new SheetReference(bent, "38 of 38 markers found"));
        session.AddShot(new PointD(1200, 900));
        var (state, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        var reopened = Assert.IsType<BentSheetMapping>(Assert.IsType<SheetReference>(state.Scale).Mapping);
        foreach (var at in new[] { new PointD(300, 400), new PointD(1250, 1650), new PointD(2300, 3000), new PointD(2450, 1500) })
        {
            var a = bent.ToPage(at);
            var b = reopened.ToPage(at);
            Assert.True(Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(a.Y - b.Y) < 1e-6, $"{at}: {a} against {b}");
        }
    }
}
