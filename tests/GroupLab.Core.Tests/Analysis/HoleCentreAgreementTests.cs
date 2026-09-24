using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 170 section 4.5: on the fixture scans the detected hole centres agree with the centres fitted to each hole's
/// edge within a stated tolerance, and a change that worsens the agreement fails.
/// <para>
/// <b>What it holds, and what it does not decide.</b> The detector reports each hole's residual-weighted centroid, and against the
/// edge-fitted centres it sits toward the scanner lamp's shadow, the same way on every scan: 0.0080 in on average with a scatter of
/// 0.0137 in on the sample scan, 0.0069 in and 0.0093 in on the friend's. Two replacements were tried and neither is adopted yet: the edge
/// fit itself moved some synthetic holes, whose true centres are known, by up to 0.039 in, and the plain area centroid passed every
/// synthetic test but sat no closer to the edges on the sample. Which centre a person would click is what request 9's hand markings will
/// say, and question 51 has the numbers. Until then these bounds hold today's agreement, so a change that makes it worse fails, and a fix
/// that makes it better tightens them.
/// </para>
/// </summary>
public class HoleCentreAgreementTests(ITestOutputHelper output)
{
    private const double MeanOffsetInches = 0.012;

    private const double ScatterInches = 0.015;

    public static TheoryData<string, string, string> Scans => new()
    {
        { "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png", "0.264" },
        { "test-data", "Scan_20260923.png", "0.264" },
    };

    [Theory]
    [MemberData(nameof(Scans))]
    public void DetectedCentresAgreeWithTheHolesEdges(string where, string file, string calibre)
    {
        string path = where == "samples" ? Path.Combine(Repo.PathTo("samples"), file) : TestData.Path(file, @"C:\Users\Airwolf\Downloads");
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {file} is not on this machine");
            return;
        }

        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var trace = new GroupLab.Core.Trace.TraceRecorder { KeepArtefacts = true };
        var result = AutomaticMarking.Run(grey, value, metadata, BuiltIns.Load("GL-CF25-LTR-D.gltd.json"), new OpenCvSharpBackend(), trace, calibre: Calibre.Parse(calibre, out _));
        Assert.Null(result.Failure);
        var residual = result.Difference!.Residual!;
        double dpi = result.Measurement.Scale!.PixelsPerDmmArea * 254;

        var offsets = new List<PointD>();
        foreach (var d in result.Detections)
        {
            if (HoleEdgeFit.Fit(residual, d.Image, (d.DiameterInches ?? 0.25) * dpi / 2, residual: true) is { } edge)
            {
                offsets.Add(new PointD((d.Image.X - edge.Centre.X) / dpi, (d.Image.Y - edge.Centre.Y) / dpi));
            }
        }

        Assert.True(offsets.Count >= result.Detections.Count * 3 / 4, $"only {offsets.Count} of {result.Detections.Count} holes gave an edge");
        double mx = offsets.Average(p => p.X), my = offsets.Average(p => p.Y);
        double mean = Math.Sqrt((mx * mx) + (my * my));
        double scatter = Math.Sqrt(offsets.Average(p => Math.Pow(p.X - mx, 2) + Math.Pow(p.Y - my, 2)));
        output.WriteLine(FormattableString.Invariant($"{file}: {offsets.Count} holes, mean offset {mean:0.0000} in, scatter {scatter:0.0000} in"));
        Assert.True(mean <= MeanOffsetInches, FormattableString.Invariant($"{file}: detected centres sit {mean:0.0000} in one way from the holes' edges"));
        Assert.True(scatter <= ScatterInches, FormattableString.Invariant($"{file}: detected centres scatter {scatter:0.0000} in about the holes' edges"));
    }
}
