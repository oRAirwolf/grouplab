using GroupLab.Cli.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// Conformance test 43 of TARGET-SCHEMA.md section 10, the Phase 0a gate of PHASE0-BRIEF.md section 3, on the real PDF:
/// PDFium rasterises every page of every built-in sheet at 300 DPI, and the reference sheet at 600 DPI and printed at 96.2
/// percent. Each render is distorted by <see cref="Perturbation.Phase0"/>, registered through OpenCV, and every bull
/// centre must come back within one thousandth of an inch.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class SyntheticScanTests(ITestOutputHelper output)
{
    private static readonly OpenCvSharpBackend Backend = new();

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string file in BuiltIns.Files)
        {
            data.Add(file);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Test43EveryBullIsRecoveredFromA300DpiRender(string file) => Check(file, 300, 1.0);

    [Fact]
    public void Test43TheReferenceSheetAt600Dpi() => Check("GL-CF25-LTR.gltd.json", 600, 1.0);

    /// <summary>DETECTION-PIPELINE.md stage S4 and FIDUCIAL-DECISION.md section 10 item 7: a 96.2 percent print is reported as such.</summary>
    [Fact]
    public void Test43APrintAt962PercentReportsItsScale()
    {
        var reports = Check("GL-CF25-LTR.gltd.json", 300, 0.962);
        Assert.All(reports, r => Assert.InRange(r.Registration.Scale, 0.9615, 0.9625));
    }

    private List<SyntheticScanReport> Check(string file, int dpi, double scale)
    {
        var d = BuiltIns.Load(file);
        var result = TargetRenderer.Render(d, new GroupLab.Core.Rendering.RenderOptions(Scale: scale));
        var reports = new List<SyntheticScanReport>();
        for (int page = 0; page < result.Pages.Count; page++)
        {
            var render = Raster.WholePage(result.Pdf!, page, dpi);
            var report = SyntheticScanCheck.Run(render, d, result.Pages[page].TileIndex, dpi, Perturbation.Phase0, Backend);
            output.WriteLine($"{file} at {scale:0.###}: {report.Summary()}");
            reports.Add(report);
        }

        Assert.All(reports, r => Assert.True(r.Passed, $"{file} at {scale:0.###}: {r.Summary()}"));
        return reports;
    }
}
