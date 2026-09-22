using GroupLab.Cli;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 113 section 4: photographs against the scan of the same sheet. The pairing and the figures on hand-made
/// positions, and the command end to end on a synthetic scan and a synthetic photograph of the same sheet, turned and at another resolution,
/// with the truth taken from the scan's detection and then from a corrected marking, and saying which.
/// </summary>
public class PhotoComparisonTests
{
    [Fact]
    public void HolesPairNearestFirstWithinTheMatchingThreshold()
    {
        var truthBulls = new Dictionary<int, PointD> { [0] = new(1000, 1000), [1] = new(2000, 1000) };
        var photoBulls = new Dictionary<int, PointD> { [0] = new(1000.5, 1000), [1] = new(2000, 1001.27) };
        PointD[] truth = [new(0, 0), new(100, 0), new(500, 500)];
        PointD[] photo = [new(2.54, 0), new(100, 12.7), new(900, 900), new(101, 0)];
        var row = PhotoComparison.Compare("p.jpg", "homography", truthBulls, truth, photoBulls, photo);
        Assert.Equal(2, row.BullsCompared);
        Assert.Equal(0.005, row.BullWorstInches!.Value, 9);
        Assert.Equal(((0.5 / 254) + 0.005) / 2, row.BullMedianInches!.Value, 9);
        // (100, 0) pairs with (101, 0), the nearer, not with (100, 12.7); (500, 500) has nothing within 0.15 in.
        Assert.Equal((3, 4, 1, 2), (row.TruthHoles, row.Found, row.Missed, row.False));
        Assert.Equal(0.01, row.HoleWorstInches!.Value, 9);
        Assert.Equal(((1 / 254.0) + 0.01) / 2, row.HoleMedianInches!.Value, 9);
        Assert.Contains(PhotoComparison.Table([row]), l => l.Contains("decides neither gate", StringComparison.Ordinal));
    }

    private static void Write(GrayImage image, string path)
    {
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        Cv2.ImWrite(path, mat);
    }

    [Fact]
    public void ThePhotographsAreMeasuredAgainstTheScanAndItSaysWhichTruth()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 300);
        var random = new Random(113);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / 300.0;
        var scan = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);

        // The photograph: the same holes, the sheet turned 1.5 degrees and seen at 280 dpi, with a margin round it.
        double p = 254 / 280.0, angle = 1.5 * Math.PI / 180, c = Math.Cos(angle) * p, n = Math.Sin(angle) * p;
        double tx = -((c * 120) - (n * 160)), ty = -((n * 120) + (c * 160));
        var photo = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([c, -n, tx, n, c, ty, 0, 0, 1])), 2700, 3450, holes, [], random);

        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-photos-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            string scanPath = Path.Combine(folder, "sheet-scan.png"), photoPath = Path.Combine(folder, "sheet-photo-1.png");
            Write(scan, scanPath);
            Write(photo, photoPath);

            var output = new StringWriter();
            Assert.Equal(0, PhotoVerb.Run([scanPath, photoPath, "--library", Repo.PathTo("targets")], output, new StringWriter()));
            var lines = output.ToString().Split(Environment.NewLine);
            Assert.StartsWith("Truth: the scan's own detection", lines[0], StringComparison.Ordinal);
            var cells = lines.Single(l => l.StartsWith("sheet-photo-1.png", StringComparison.Ordinal)).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(25, int.Parse(cells[5], System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(("25", "0", "0"), (cells[6], cells[7], cells[8]));
            Assert.True(double.Parse(cells[3], System.Globalization.CultureInfo.InvariantCulture) < 0.01, cells[3]);
            Assert.True(double.Parse(cells[11], System.Globalization.CultureInfo.InvariantCulture) < 0.03, cells[11]);

            // A corrected marking is the truth once there is one: here the person has taken one hole away, so the photograph has one false.
            var marking = AnalyzeVerb.Analyze(scanPath, null, out _, [Repo.PathTo("targets")])!.Marking!;
            var corrected = marking with { Shots = marking.Shots.Remove(marking.Shots.First(s => s.IsShot)) };
            string truthPath = Path.Combine(folder, "sheet-scan.grouplab.json");
            File.WriteAllText(truthPath, MarkingFile.Write(corrected));
            output = new StringWriter();
            Assert.Equal(0, PhotoVerb.Run([scanPath, photoPath, "--truth", truthPath, "--library", Repo.PathTo("targets")], output, new StringWriter()));
            lines = output.ToString().Split(Environment.NewLine);
            Assert.Equal("Truth: the corrected scan marking, sheet-scan.grouplab.json.", lines[0]);
            cells = lines.Single(l => l.StartsWith("sheet-photo-1.png", StringComparison.Ordinal)).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(("24", "25", "0", "1"), (cells[5], cells[6], cells[7], cells[8]));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
