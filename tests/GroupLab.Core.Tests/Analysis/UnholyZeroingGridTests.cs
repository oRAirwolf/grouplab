using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 191 and 193: Unholy's scan of GroupLab's own "Zeroing Grid, mil at 100 yd", printed at actual size on
/// Letter and scanned at 600 dpi, with one shot in it just below and right of the point of aim. Nightlies 95 and 99 registered all sixteen
/// markers and found no hole, because the grid's one bull had a cell of no size and every hole was refused as out of place (entry 189).
/// <para>
/// It is not <c>Scan_20260923.png</c>, whose name is almost the same. The original's SHA-256 is 367e55e5...7d73; the copy published on the
/// test-data release, rebuilt from its pixels with only the resolution kept, is c6db8580...7b27. Consent: entry 190, in
/// <c>samples/PROVENANCE.md</c>.
/// </para>
/// </summary>
public class UnholyZeroingGridTests
{
    private const string File = "zeroing-grid-mil-100yd-unholy-2026-09-24.png";
    private const string Rebuilt = "c6db8580f59412b5152f6d8520c2e9143c174ce397606095cbb4c33e51367b27";

    [Fact]
    public void TheOneShotOnUnholysZeroingGridIsFound()
    {
        string path = TestData.Path(File, @"C:\Dev\grouplab-submissions\unholy");
        if (!System.IO.File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not on this machine, so Unholy's zeroing grid was not checked");
            return;
        }

        Assert.True(TestData.Matches(path, Rebuilt), $"{path} is not the published copy of Unholy's zeroing grid scan");
        var definition = BuiltIns.Load("GL-ZERO-MIL-100Y.gltd.json");
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);

        // Entry 195 section 4, question 56: it names itself from its own codes, all four of them, rather than asking which sheet it is.
        var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates([Repo.PathTo("targets")]), new OpenCvSharpBackend(), new TraceRecorder());
        Assert.True(identity.Failure is null, identity.Failure);
        Assert.Equal(definition.Name, identity.Definition?.Name);
        Assert.Equal(4, identity.CodesRead);

        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend());

        Assert.True(result.Failure is null, result.Failure);
        Assert.Equal(16, result.Measurement.Fiducials?.Matches.Count);
        // Printed at actual size: the markers say 600 dpi at the sheet, which is what it was scanned at.
        Assert.InRange(result.Measurement.Fiducials!.PixelsPerDmm * 254, 598, 602);
        var hole = Assert.Single(result.Detections);
        Assert.InRange(hole.Image.X, 2586, 2606);
        Assert.InRange(hole.Image.Y, 3125, 3145);
    }
}
