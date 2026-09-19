using GroupLab.Cli;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 111 section 4: the paths the range material of 20 September will need, ready before it arrives. Detection
/// over a folder from one command, and the Phase 3 timing read from the application's own log.
/// </summary>
public class FolderVerbsTests
{
    /// <summary>The log lines the window writes for one sheet: opened, detected, two choices and a typed bull, accepted with one item open.</summary>
    [Fact]
    public void TheTimingReadsEachSheetFromOpeningToAccept()
    {
        string[] log =
        [
            "2026-09-20T15:00:00.000Z  INFO   app.window     scale=1",
            "2026-09-20T15:00:01.000Z  INFO   image.open     file=sheet-1.png pathid=0a1b2c3d format=PNG",
            "2026-09-20T15:00:13.500Z  INFO   detect.run     ms=12400 stages=12 summary=\"38 of 38 markers found\"",
            "2026-09-20T15:00:20.000Z  INFO   review.choose  kind=Contested action=KeepMatched",
            "2026-09-20T15:00:25.000Z  INFO   review.typed   bull=14",
            "2026-09-20T15:00:31.000Z  INFO   review.choose  kind=Oversized action=OneShot",
            "2026-09-20T15:01:03.500Z  INFO   analysis.accept open=1",
            "2026-09-20T15:02:00.000Z  INFO   image.open     file=\"sheet 2.png\" pathid=1b2c3d4e format=PNG",
        ];
        var sheets = FolderVerbs.Sheets(log);
        Assert.Equal(2, sheets.Count);
        var first = sheets[0];
        Assert.Equal("sheet-1.png", first.File);
        Assert.Equal(50.0, (first.Accepted!.Value - first.Detected!.Value).TotalSeconds, 6);
        Assert.Equal(62.5, (first.Accepted.Value - first.Opened).TotalSeconds, 6);
        Assert.Equal(3, first.Choices);
        Assert.Equal(1, first.OpenAtAccept);
        Assert.Equal("sheet 2.png", sheets[1].File);
        Assert.Null(sheets[1].Accepted);
    }

    /// <summary>A folder holding one rendered scan with a hole on every bull, and a file that is not an image, analysed from one command.</summary>
    [Fact]
    public void AFolderOfScansIsAnalysedOneLineEach()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var random = new Random(111);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        var image = SyntheticSheet.Compose(render, dpi, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            using (var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels))
            {
                Cv2.ImWrite(Path.Combine(folder, "sheet-1.png"), mat);
            }

            File.WriteAllText(Path.Combine(folder, "notes.txt"), "not an image");
            var output = new StringWriter();
            string markings = Path.Combine(folder, "markings");
            Assert.Equal(0, FolderVerbs.AnalyzeFolder(folder, ["--library", Repo.PathTo("targets"), "--markings", markings], output, new StringWriter()));
            string text = output.ToString();
            Assert.Contains("sheet-1.png", text, StringComparison.Ordinal);
            Assert.Contains("1 analysed, 0 failed, of 1 images", text, StringComparison.Ordinal);
            Assert.DoesNotContain("notes.txt", text, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(markings, "sheet-1.grouplab.json")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
