using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.1: a set of sheets from "Made for your optic", rendered with one hole on every bull, each sheet
/// read by the real pipeline, and pooled in any order. Each sheet knows which of the set it is from its own codes; the set says which are
/// missing, counts a sheet read twice once, and pools every shot about its own bull.
/// </summary>
public class SetPoolTests
{
    private const double Dpi = 300;

    private static TargetDefinition Set()
    {
        var made = TargetGenerator.Generate(new GeneratorRequest(100, 4, null, 25, "letter"));
        Assert.True(made.Sheets > 2, $"the 4x set is {made.Sheets} sheets");
        return made.Design!.Definition!;
    }

    /// <summary>One sheet of the set, rendered, holed and read; its marking.</summary>
    private static MarkingState Read(TargetDefinition set, int sheet, int seed)
    {
        var scene = SceneBuilder.Build(set);
        var render = SceneRasterizer.Rasterize(scene.Pages[sheet], Dpi);
        var random = new Random(seed);
        var holes = set.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-60, 61), b.Y + random.Next(-60, 61), onInk: false, HoleBacking.ScannerLid, 1.0)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, set, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        Assert.True(result.Failure is null, result.Failure);
        Assert.Equal(sheet, result.SetSheet);
        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, setSheet: result.SetSheet);
        return session.State;
    }

    [Fact]
    public void TheSheetsOfASetPoolInAnyOrderAndSayWhichAreMissing()
    {
        var set = Set();
        int size = set.Tiling!.Cols * set.Tiling.Rows, perSheet = set.Bulls.Count(b => b.Scoring);
        var read = new List<MarkingState> { Read(set, 3, 1), Read(set, 0, 2), Read(set, 5, 3), Read(set, 0, 4), Read(set, 2, 5) };

        var pooled = SetPool.Pool(set, read);
        Assert.Equal(size, pooled.SetSize);
        Assert.Equal([0, 2, 3, 5], pooled.Found);
        Assert.Equal(Enumerable.Range(0, size).Except([0, 2, 3, 5]), pooled.Missing);
        Assert.Equal([0], pooled.Repeated);
        Assert.Equal(4 * perSheet, pooled.Shots);
        Assert.Equal(size * perSheet, pooled.BullsInSet);
        Assert.StartsWith("Sheets 1, 3, 4 and 6 of " + size, pooled.Said, StringComparison.Ordinal);
        Assert.Contains("read more than once", pooled.Said, StringComparison.Ordinal);

        // Every shot is within the scatter the holes were placed with, about its own bull: 60 dmm is 0.24 in each way.
        Assert.NotNull(pooled.Figures);
        Assert.InRange(pooled.Figures!.MeanRadius!.Value, 0.03, 0.30);
    }

    [Fact]
    public void AMarkingKeepsWhichSheetOfTheSetItIs()
    {
        var set = Set();
        var state = Read(set, 1, 9);
        var (back, _) = MarkingFile.Read(MarkingFile.Write(state));
        Assert.Equal(1, back.SetSheet);
    }
}
