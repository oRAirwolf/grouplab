using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 189 section 4: Unholy's "Zeroing Grid, mil at 100 yd" could not be detected, and it is one of GroupLab's own
/// sheets. Every sheet in the library, rendered as it prints at 300 dpi with holes on it, names itself from its own codes and is detected
/// and analyzed end to end, finding every hole. If this passes for a sheet somebody could not read, the fault is in their print or their
/// photograph, not in the sheet.
/// </summary>
public class EverySheetDetectsTests
{
    private const double Dpi = 300;

    private static readonly IReadOnlyList<TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    public static TheoryData<string> Sheets()
    {
        var data = new TheoryData<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("targets"), "*.gltd.json").Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal))
        {
            if (!file.Contains(".3x2.", StringComparison.Ordinal))
            {
                data.Add(file);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Sheets))]
    public void EverySheetInTheLibraryDetectsFromItsOwnRender(string file)
    {
        var definition = BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var random = new Random(189);

        // Holes on up to five scoring bulls, each a little off center, as a group of shots lands. On a zeroing grid, one bull, the group
        // is five holes in the open cells around it, because the grid's axes cross at the point of aim. A hole says whether it is on
        // ink, read from the render, since a hole through a printed line looks different from one through paper.
        var aimed = definition.Bulls.Where(b => b.Scoring).ToList();
        Assert.True(aimed.Count > 0, $"{file} has no scoring bull to shoot at");
        IEnumerable<(double X, double Y)> at = definition.Grids is { Count: > 0 } grids
            ? new (double, double)[] { (1.5, 0.5), (-1.5, 0.5), (0.5, 1.5), (1.5, -1.5), (-0.5, -1.5) }
                .Select(c => (aimed[0].X + (c.Item1 * grids[0].Half / grids[0].Divisions), aimed[0].Y + (c.Item2 * grids[0].Half / grids[0].Divisions)))
            : aimed.Take(5).Select(b => ((double)b.X + random.Next(-40, 41), (double)b.Y + random.Next(-40, 41))).ToList();
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var holes = at.Select(p => SyntheticSheet.SampleHole(random, p.X, p.Y, OnInk(p.X, p.Y), HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);

        var backend = new OpenCvSharpBackend();
        // The codes name the sheet or they do not read; they never name another sheet. Where they do not read, the application asks which
        // sheet it is and goes on, and so does this. Question 56: on two or three sheets they read on one platform's OpenCV and not another's.
        var identity = SheetIdentification.Identify(image, Library, backend, new TraceRecorder());
        Assert.True(identity.Failure is not null || identity.DefinitionId == GltdBinary.Encode(definition).Encoding!.DefinitionId,
            $"{file} was named as another sheet, {identity.DefinitionId}");

        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, backend);
        Assert.True(result.Failure is null, $"{file} was not analyzed: {result.Failure}");
        Assert.True(result.Detections.Count == holes.Count, $"{file}: {result.Detections.Count} holes found of {holes.Count}. {result.Summary}");
    }

    /// <summary>
    /// Entry 193 section 3: on a zeroing grid the shots land on the lines as often as between them. Five holes on each of the four grids,
    /// one centered on a thin line, one on the other direction's, one on a crossing, and one on each thick axis, are all found.
    /// </summary>
    [Theory]
    [InlineData("GL-ZERO-MIL-100Y.gltd.json")]
    [InlineData("GL-ZERO-MIL-100M.gltd.json")]
    [InlineData("GL-ZERO-MOA-100Y.gltd.json")]
    [InlineData("GL-ZERO-MOA-100M.gltd.json")]
    public void HolesOnAZeroingGridsLinesAreFound(string file)
    {
        var definition = BuiltIns.Load(file);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var random = new Random(193);
        var grid = definition.Grids![0];
        double pitch = (double)grid.Half / grid.Divisions;
        var at = new (double, double)[] { (1.0, 0.4), (0.4, -1.0), (2.0, 2.0), (0.0, 1.6), (-1.6, 0.0) }
            .Select(c => (X: grid.CentreX + (c.Item1 * pitch), Y: grid.CentreY + (c.Item2 * pitch))).ToList();
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var holes = at.Select(p => SyntheticSheet.SampleHole(random, p.X, p.Y, OnInk(p.X, p.Y), HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend());
        Assert.True(result.Failure is null, $"{file} was not analyzed: {result.Failure}");
        Assert.True(result.Detections.Count == holes.Count, $"{file}: {result.Detections.Count} holes found of {holes.Count} on and across its lines. {result.Summary}");
    }

    /// <summary>Entry 193 section 4: a sheet found with no holes on it says so, with how to mark them by hand, never a blank result.</summary>
    [Fact]
    public void ASheetWithNoHolesSaysSoAndHowToMarkThem()
    {
        var definition = BuiltIns.Load("GL-ZERO-MIL-100Y.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var metadata = new ImageMetadata("PNG", render.Width, render.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(render, render, metadata, definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        Assert.Empty(result.Detections);
        string? said = DetectionAdvice.NoHoles(result, calibre: null);
        Assert.NotNull(said);
        Assert.StartsWith($"GroupLab found {definition.Name} and no holes on it.", said, StringComparison.Ordinal);
        Assert.Contains("No caliber was entered", said, StringComparison.Ordinal);
        Assert.EndsWith("mark them by hand: choose Impact, or press I, and click each hole.", said, StringComparison.Ordinal);
        Assert.DoesNotContain("No caliber", DetectionAdvice.NoHoles(result, Calibre.Of(0.308))!, StringComparison.Ordinal);
    }
}
