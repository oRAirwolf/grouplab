using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 358 section 3: the X6 label family, six of the 25-bull Letter sheets' bulls to a 4x6, A6 or 100 x 150 mm
/// label, 2 by 3 at 38.0 mm, a marker on every cell corner, one code in a band at the top, five labels a set; and the printer check label,
/// which measures a thermal printer on its own paper, across and along the feed. Every label is read back from the dots a 203 dpi thermal
/// printer would print.
/// </summary>
public class LabelSheetTests(ITestOutputHelper output)
{
    private const double Dpi = 203.2;

    private static readonly IReadOnlyList<TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    public static TheoryData<string> Sets() => [.. LibraryBuilder.LabelPages.SelectMany(p => new[] { p.Stem, p.Stem + "-C", p.Stem + "-E" })];

    public static TheoryData<string> CheckLabels() => ["GL-SCALE-4X6-1", "GL-SCALE-A6-1", "GL-SCALE-100X150-1"];

    [Fact]
    public void TheLibraryHoldsNineSetsThreeLabelSizesAndAllThreeBulls()
    {
        var files = BuiltIns.Files.Where(f => f.StartsWith("GL-X6-", StringComparison.Ordinal)).ToList();
        Assert.Equal(9, files.Count);
        var sheets = TargetLibrary.Load(Repo.PathTo("targets")).Where(s => s.File.StartsWith("GL-X6-", StringComparison.Ordinal)).ToList();
        Assert.All(sheets, s => Assert.Equal("Thermal labels", s.Family));
        Assert.Equal(3, sheets.Count(s => s.Definition.Page.Size == PageSize.Label4x6));
        Assert.Equal(3, sheets.Count(s => s.Definition.Page.Size == PageSize.A6));
        Assert.Equal(3, sheets.Count(s => s.Definition.Page.Size == PageSize.Label100x150));
    }

    /// <summary>Alan: no bull smaller than the 25-bull Letter sheets' bull, the 1.00 in ring stack or the 1.25 in C diamond.</summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void SixBullsTwoByThreeAtTheLetterPitchNoneSmallerThanTheLetterBull(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var letter = BuiltIns.Load((stem.EndsWith("-C", StringComparison.Ordinal) ? "GL-CF25-LTR-C" : stem.EndsWith("-E", StringComparison.Ordinal) ? "GL-CF25-LTR-E" : "GL-CF25-LTR") + ".gltd.json");
        Assert.Equal(6, d.Bulls.Count(b => b.Scoring));
        Assert.Equal(2, d.Bulls.Select(b => b.X).Distinct().Count());
        Assert.Equal(3, d.Bulls.Select(b => b.Y).Distinct().Count());
        Assert.Equal([380], d.Bulls.Select(b => b.X).Distinct().Order().Zip(d.Bulls.Select(b => b.X).Distinct().Order().Skip(1), (a, b) => b - a).Distinct());
        Assert.True(d.RingSets.Single().Discs.Max(x => x.Diameter) >= letter.RingSets.Single().Discs.Max(x => x.Diameter));
        Assert.Equal(new Tiling(5, 1, d.Page.Width, d.Page.Height, 0), d.Tiling);
        Assert.Equal(12, d.Fiducials!.Markers!.Count);
        Assert.Equal("grid-boundary-1", d.Fiducials.Scheme);
    }

    /// <summary>
    /// The real payload, measured: every label's frame fits the one code it prints. Measured on 2026-10-03: 76 bytes with the ringed bull and
    /// 70 with the C or E bull, against 84 for version 8 at level H on 4x6 and 86 for version 7 at level Q on A6 and 100 x 150 mm.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void TheFrameFitsTheOneCodeAtTheTop(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var c = d.Codes!;
        Assert.Equal((1, CodePlacement.Explicit, 4), (c.Count, c.Placement, c.ModuleSize));
        var encoding = GltdBinary.Encode(d).Encoding!;
        int frame = GltdBinary.ReplicatedFrame(encoding).Length;
        int capacity = (c.Version, c.EcLevel) switch { (8, EcLevel.H) => 84, (7, EcLevel.Q) => 86, _ => 0 };
        output.WriteLine($"{stem}: frame {frame} bytes, version {c.Version} level {c.EcLevel} holds {capacity}");
        Assert.InRange(frame, 1, capacity);
        Assert.Equal(stem.EndsWith("-C", StringComparison.Ordinal) || stem.EndsWith("-E", StringComparison.Ordinal) ? 70 : 76, frame);
        Assert.Equal(d.Page.Size == PageSize.Label4x6 ? 8 : 7, c.Version);
    }

    /// <summary>The code's band: the margin above it and the margin below the bottom markers are equal, and 2 mm separate it from the markers.</summary>
    [Theory]
    [InlineData("GL-X6-4X6", 38)]
    [InlineData("GL-X6-A6", 24)]
    [InlineData("GL-X6-100X150", 34)]
    public void TheCodeBandAndTheMarginsAreAsLaidOut(string stem, int margin)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var c = d.Codes!;
        int footprint = (((4 * c.Version!.Value) + 17) * c.ModuleSize) + (2 * c.QuietZone!.Value);
        int top = c.Positions[0].Y - (footprint / 2);
        int markersTop = d.Fiducials!.Markers!.Min(m => m.Y) - 30, markersBottom = d.Fiducials.Markers!.Max(m => m.Y) + 30;
        Assert.Equal(margin, top);
        Assert.Equal(margin, d.Page.Height - markersBottom);
        Assert.Equal(LibraryBuilder.LabelCodeGap, markersTop - (top + footprint));
    }

    /// <summary>
    /// Every label of the 4x6 set, and the first of every other set, as a 203 dpi thermal printer prints it, with holes in it: it names
    /// itself and its place in the set from its one code, says "Label n of 5", and every hole is found.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void EveryLabelReadsItselfBackFromItsThermalPrintWithEveryHole(string stem)
    {
        var definition = BuiltIns.Load(stem + ".gltd.json");
        var pages = SceneBuilder.Build(definition).Pages;
        Assert.Equal(5, pages.Count);
        var backend = new OpenCvSharpBackend();
        foreach (var page in stem == "GL-X6-4X6" ? pages : pages.Take(1))
        {
            Assert.Contains(page.Items.OfType<TextRun>(), t => t.Text == $"Label {page.TileIndex + 1} of 5");
            var render = ThermalRaster.Render(page, new PrintHead(Dpi, 2000)).Image.ToGray();
            var random = new Random(358);
            bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
            var holes = definition.Bulls.Where(b => b.Scoring).Select(b => ((double)b.X + random.Next(-40, 41), (double)b.Y + random.Next(-40, 41))).ToList()
                .Select(p => SyntheticSheet.SampleHole(random, p.Item1, p.Item2, OnInk(p.Item1, p.Item2), HoleBacking.ScannerLid, 0.871)).ToList();
            double s = 254 / Dpi;
            var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
            var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);

            var identity = SheetIdentification.Identify(image, Library, backend, new TraceRecorder());
            Assert.True(identity.Failure is null, $"{stem} label {page.TileIndex + 1} did not name itself: {identity.Failure}");
            Assert.Equal(GltdBinary.Encode(definition).Encoding!.DefinitionId, identity.DefinitionId);
            Assert.Equal(page.TileIndex, identity.TileIndex);

            var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
            var result = AutomaticMarking.Run(image, image, metadata, definition, backend);
            Assert.True(result.Failure is null, $"{stem} label {page.TileIndex + 1} was not analyzed: {result.Failure}");
            Assert.Equal(holes.Count, result.Detections.Count);
            Assert.Equal(page.TileIndex, result.SetSheet);
        }
    }

    /// <summary>A label's words stand in the band beside its code, above its markers, where render-and-difference never looks.</summary>
    [Theory]
    [MemberData(nameof(Sets))]
    public void TheWordsStandInTheBandAboveTheMarkers(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        var page = SceneBuilder.Build(d, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages[0];
        long markersTop = 2L * (d.Fiducials!.Markers!.Min(m => m.Y) - 30);
        var words = page.Items.OfType<TextRun>().Where(t => t.Layer is SceneLayer.Name or SceneLayer.Identifier or SceneLayer.PrintNote).ToList();
        Assert.Equal(5, words.Count);
        Assert.All(words, t => Assert.True(t.Baseline + (t.FontSize / 4) < markersTop, $"{stem}: {t.Text} reaches the markers"));
        Assert.All(words, t => Assert.True(t.X >= 2L * (d.Codes!.Positions[0].X + 100), $"{stem}: {t.Text} overlaps the code"));
    }

    [Fact]
    public void FiveLabelsHoldThirtyBullsAndAMissingLabelIsNamed()
    {
        var pooled = SetPool.Pool(BuiltIns.Load("GL-X6-4X6.gltd.json"), []);
        Assert.Equal(5, pooled.SetSize);
        Assert.Equal(30, pooled.BullsInSet);
        Assert.StartsWith("None of the 5 labels of this set has been read yet.", pooled.Said, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CheckLabels))]
    public void TheCheckLabelIsACheckPageWithItsSpansAcrossAndAlongTheFeed(string stem)
    {
        var d = BuiltIns.Load(stem + ".gltd.json");
        Assert.True(PrinterCheck.IsCheckPage(d));
        Assert.True(PageSizes.IsLabel(d.Page.Size));
        var cross = GridStyle4.Crosshairs(d.Page);
        Assert.Equal(800, cross[1].X - cross[0].X);
        Assert.Equal(400, cross[2].Y - cross[1].Y);
        Assert.Equal((800, 400), (GridStyle4.CaliperAcross(d.Page), GridStyle4.CaliperDown(d.Page)));
        Assert.Equal(8, d.Fiducials!.Markers!.Count);
        Assert.InRange(GltdBinary.ReplicatedFrame(GltdBinary.Encode(d).Encoding!).Length, 1, 86);
        Assert.Equal((1500, 1500), (GridStyle4.CaliperAcross(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json").Page), GridStyle4.CaliperDown(BuiltIns.Load("GL-SCALE-LTR-1.gltd.json").Page)));
    }

    /// <summary>
    /// A thermal printer whose feed runs 1.5 percent short on its paper: the card on the check label measures it across and along the feed
    /// separately, and the profile it makes belongs to that printer on that paper.
    /// </summary>
    [Theory]
    [MemberData(nameof(CheckLabels))]
    public void ACardOnTheCheckLabelMeasuresThePrinterOnItsPaperBothWays(string stem)
    {
        const double across = 1.000, along = 0.985, dpi = 200;
        var definition = BuiltIns.Load(stem + ".gltd.json");
        var flat = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi * across, words: true);
        using var source = OpenCvSharp.Mat.FromPixelData(flat.Height, flat.Width, OpenCvSharp.MatType.CV_8UC1, flat.Pixels);
        using var stretched = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Resize(source, stretched, new OpenCvSharp.Size(flat.Width, (int)Math.Round(flat.Height * along / across)), 0, 0, OpenCvSharp.InterpolationFlags.Area);
        stretched.GetArray(out byte[] pixels);
        int width = stretched.Width, height = stretched.Height;
        double pxPerMm = dpi / 25.4;
        var card = GridStyle4.Card(definition.Page);
        double cx = (card.X + (GridStyle4.CardWidthDmm / 2.0)) / 10.0 * across * pxPerMm, cy = (card.Y + (GridStyle4.CardHeightDmm / 2.0)) / 10.0 * along * pxPerMm;
        double hw = GridStyle4.CardWidthMm / 2 * pxPerMm, hh = GridStyle4.CardHeightMm / 2 * pxPerMm, r = 3.18 * pxPerMm;
        for (int y = (int)(cy - hh - 4); y < cy + hh + 4; y++)
        {
            for (int x = (int)(cx - hw - 4); x < cx + hw + 4; x++)
            {
                int inside = 0;
                foreach (var (ox, oy) in new[] { (0.25, 0.25), (0.75, 0.25), (0.25, 0.75), (0.75, 0.75) })
                {
                    double u = x + ox - cx, v = y + oy - cy;
                    double qx = Math.Max(Math.Abs(u) - (hw - r), 0), qy = Math.Max(Math.Abs(v) - (hh - r), 0);
                    inside += Math.Abs(u) <= hw && Math.Abs(v) <= hh && (qx * qx) + (qy * qy) <= r * r ? 1 : 0;
                }

                int i = (y * width) + x;
                pixels[i] = (byte)Math.Round(((pixels[i] * (4 - inside)) + (60 * inside)) / 4.0);
            }
        }

        var image = new GrayImage(width, height, pixels);
        var metadata = new ImageMetadata("JPEG", width, height, null, null, "Test", "Phone", 1, null, null);
        var result = PrinterCheck.Measure(image, metadata, definition, new OpenCvSharpBackend(), "Label printer", new DateOnly(2026, 10, 3));
        Assert.True(result.Profile is not null, result.Why);
        output.WriteLine($"{stem}: across {result.Profile.Across:0.0000}, along the feed {result.Profile.Down:0.0000}");
        Assert.InRange(result.Profile.Across, across - 0.003, across + 0.003);
        Assert.InRange(result.Profile.Down, along - 0.003, along + 0.003);
        Assert.Equal(PrinterProfile.PaperOf(definition.Page.Size), result.Profile.Paper);
        Assert.StartsWith("Label printer, ", result.Profile.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void APhotographOfALabelIsCorrectedByTheCheckOnThatPaper()
    {
        var office = new PrinterProfile("Office", 0.99, 0.99, PrinterMethod.Card, new DateOnly(2026, 10, 1), 0.003);
        var label = new PrinterProfile("Label printer", 1.0, 0.985, PrinterMethod.Card, new DateOnly(2026, 10, 3), 0.003).OnPaper(PageSize.Label4x6);
        Assert.Equal("Label printer, 4x6 labels", label.Name);
        Assert.Equal("4x6 labels", PrinterProfile.FromJson(label.ToJson())!.Paper);
        Assert.Same(label, PrinterProfile.For(PageSize.Label4x6, [office, label], office));
        Assert.Same(office, PrinterProfile.For(PageSize.Letter, [office, label], label));
        Assert.Same(office, PrinterProfile.For(PageSize.A6, [office, label], office));
        Assert.Null(PrinterProfile.For(PageSize.A6, [label], label));
    }
}
