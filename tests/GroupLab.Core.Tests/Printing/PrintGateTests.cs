using System.Globalization;
using System.Text;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 1.1: the print path held by tests that would fail if a night's work broke it, and that need no
/// printer to run.
/// <para>
/// <b>Why this exists and why it exists now.</b> Alan prints his targets from the newest nightly before he goes shooting. Printing is the one
/// thing that cannot slip, and the two tests that covered it best needed a real printer driver, so they skip on CI and they skipped here the
/// moment "Microsoft Print to PDF" was missing. Everything below runs anywhere: it renders through <see cref="TargetRenderer"/>, which is the
/// same call <c>PrintWindow.SavePdf</c> makes, and then reads the PDF that came out.
/// </para>
/// <para>
/// <b>The scale check is the one that matters.</b> A sheet that prints at 96 percent still looks perfect; every measurement taken from it is
/// then wrong by four percent, and nothing on the paper says so. So a distance between two markers, whose positions the definition states, is
/// measured back out of the rendered PDF and held to five thousandths of an inch.
/// </para>
/// </summary>
public class PrintGateTests
{
    /// <summary>How far a measured distance may sit from the declared one, inches. Entry 141 section 1.1 sets it.</summary>
    private const double TolerablePrintedInches = 0.005;

    private const int MeasuringDpi = 300;

    /// <summary>Every sheet in the library, and the designed sheet, as the print screen offers them.</summary>
    public static TheoryData<string> Sheets()
    {
        var data = new TheoryData<string>();
        foreach (string file in BuiltIns.Files)
        {
            data.Add(file);
        }

        return data;
    }

    private static RenderResult Rendered(TargetDefinition definition, bool filled = false) =>
        TargetRenderer.Render(definition, new RenderOptions(
            Mode: filled ? DataBlockMode.Filled : DataBlockMode.Blank,
            Instance: filled ? new Instance("A1B2", "2026-09-22", null) : null,
            PrintNote: SceneBuilder.ActualSizeNote));

    /// <summary>
    /// Every sheet renders, with no error, into as many pages as it has tiles, blank and filled. A sheet that cannot be printed is the whole
    /// failure this is here to catch, and it has to be caught for the filled path too, because that is the one a person uses.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void EverySheetRendersAPdfWithNoError(string file)
    {
        var definition = BuiltIns.Load(file);
        foreach (bool filled in new[] { false, true })
        {
            if (filled && definition.DataBlock is null)
            {
                continue;
            }

            var result = Rendered(definition, filled);
            var errors = result.Diagnostics.Where(d => d.Severity == Severity.Error).ToList();
            Assert.True(errors.Count == 0, $"{file} ({(filled ? "filled" : "blank")}): " + string.Join("; ", errors.Select(e => e.Message)));
            Assert.NotNull(result.Pdf);
            Assert.NotEmpty(result.Pages);
            Assert.Equal(definition.Tiling is { } tiling ? tiling.Cols * tiling.Rows : 1, result.Pages.Count);
            Assert.Equal(result.Pages.Count, PageBoxes(result.Pdf!).Count);
        }
    }

    /// <summary>
    /// The page in the PDF is the page the sheet asks for, to within a rounding of a point. Letter is 612 by 792 points, and a sheet that
    /// says A4 gets A4: a letter sheet silently written onto A4 is a sheet that prints scaled, which is the fault above.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void EveryPageIsTheSizeTheSheetAsksFor(string file)
    {
        var definition = BuiltIns.Load(file);
        var result = Rendered(definition);

        // The definition states the page in tenths of a millimetre, as everything else here is.
        double wantWide = definition.Page.Width / 254.0 * 72, wantHigh = definition.Page.Height / 254.0 * 72;
        foreach (var (wide, high) in PageBoxes(result.Pdf!))
        {
            Assert.Equal(wantWide, wide, 1);
            Assert.Equal(wantHigh, high, 1);
        }

        if (definition.Page.Size == PageSize.Letter)
        {
            Assert.Equal(612, PageBoxes(result.Pdf!)[0].Wide, 1);
            Assert.Equal(792, PageBoxes(result.Pdf!)[0].High, 1);
        }
    }

    /// <summary>
    /// The markers and the codes are on the page. Without them the sheet still prints and still looks right, and GroupLab cannot read it at
    /// all afterwards, which is the quietest way for a night's work to ruin a range day.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void TheMarkersAndTheCodesAreOnEveryRenderedPage(string file)
    {
        var definition = BuiltIns.Load(file);
        var result = Rendered(definition);

        foreach (var page in result.Pages)
        {
            Assert.True(page.Items.Any(i => i.Layer == SceneLayer.Markers), $"{file}: nothing on the markers layer, so nothing can register this sheet");
            Assert.Equal(definition.Codes is { Count: > 0 }, page.Items.Any(i => i.Layer == SceneLayer.Codes));
            Assert.True(page.Items.Any(i => i.Layer == SceneLayer.Bulls), $"{file}: nothing on the bulls layer");
        }
    }

    /// <summary>
    /// Entry 141 section 1.1's own measure: a known distance on the sheet measures the same in the PDF to within five thousandths of an inch.
    /// <para>
    /// The distance is between two markers the definition places itself, so nothing here is measured against a number this test invented. The
    /// PDF is rasterised by PDFium at 300 dpi and the markers are found by the same detector that reads a scan, so what is being checked is
    /// the whole path from the definition to the paper.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void AKnownDistanceMeasuresTheSameInThePdf(string file)
    {
        var definition = BuiltIns.Load(file);
        var result = Rendered(definition);
        // A sheet with no stored markers would let this pass by doing nothing, which is worse than not having the test.
        Assert.True(definition.Fiducials?.Markers is { Count: >= 2 }, $"{file}: the definition stores no markers, so there is no known distance to measure");
        var declared = definition.Fiducials!.Markers!;

        var image = Raster.WholePage(result.Pdf!, 0, MeasuringDpi);
        double side = definition.Fiducials.MarkerSize / 254.0 * MeasuringDpi;
        var found = new OpenCvSharpBackend().DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, side));

        var centres = found.Markers
            .GroupBy(m => m.Id)
            .ToDictionary(g => g.Key, g => new PointD(g.First().Corners.Average(c => c.X), g.First().Corners.Average(c => c.Y)));
        var pairs = declared.Where(m => centres.ContainsKey(m.Id)).OrderBy(m => m.Id).ToList();
        Assert.True(pairs.Count >= 2, $"{file}: {pairs.Count} of {declared.Count} declared markers were found in the rendered PDF");

        // The two furthest apart, because a long distance is where a scale error shows and a short one hides it.
        var (a, b) = Furthest(pairs);
        double onTheSheet = Math.Sqrt(Math.Pow(a.X - b.X, 2.0) + Math.Pow(a.Y - b.Y, 2.0)) / 254.0;
        double inThePdf = Math.Sqrt(Math.Pow(centres[a.Id].X - centres[b.Id].X, 2) + Math.Pow(centres[a.Id].Y - centres[b.Id].Y, 2)) / MeasuringDpi;

        Assert.True(Math.Abs(inThePdf - onTheSheet) <= TolerablePrintedInches,
            string.Create(CultureInfo.InvariantCulture,
                $"{file}: markers {a.Id} and {b.Id} are {onTheSheet:0.0000} in apart on the sheet and {inThePdf:0.0000} in apart in the PDF, out by {inThePdf - onTheSheet:0.0000} in"));
    }

    private static (Marker A, Marker B) Furthest(IReadOnlyList<Marker> markers)
    {
        var best = (A: markers[0], B: markers[1]);
        double far = -1;
        for (int i = 0; i < markers.Count; i++)
        {
            for (int j = i + 1; j < markers.Count; j++)
            {
                double d = Math.Pow(markers[i].X - markers[j].X, 2.0) + Math.Pow(markers[i].Y - markers[j].Y, 2.0);
                if (d > far)
                {
                    (far, best) = (d, (markers[i], markers[j]));
                }
            }
        }

        return best;
    }

    /// <summary>Every page's media box, in points, read out of the PDF itself rather than from what the renderer says it wrote.</summary>
    private static IReadOnlyList<(double Wide, double High)> PageBoxes(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        var boxes = new List<(double, double)>();
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            text, @"/MediaBox\s*\[\s*(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)\s*\]"))
        {
            double x0 = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            double y0 = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double x1 = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            double y1 = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            boxes.Add((x1 - x0, y1 - y0));
        }

        Assert.NotEmpty(boxes);
        return boxes;
    }
}
