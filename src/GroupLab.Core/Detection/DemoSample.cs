using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 439 and 446: the guided demo's sample, made by GroupLab as the tests make theirs: the 25-bull Letter sheet
/// as it prints, with one hole a bull drawn the way a scanner sees one. So it is nobody's target, no file has to be shipped, and the desktop
/// and both phones make the same sheet from this one place (question 101, accepted 2026-10-18).
/// </summary>
public static class DemoSample
{
    /// <summary>The sheet the sample is drawn from, by its file name in the library.</summary>
    public const string SheetFile = "GL-CF25-LTR.gltd.json";

    /// <summary>The name the sample is written under, in a folder of the application's own.</summary>
    public const string FileName = "grouplab-demo-sample-sheet.png";

    /// <summary>The scan's resolution, dots an inch.</summary>
    public const double Dpi = 300;

    /// <summary>The sample's pixels: the same on every machine, from the same seed.</summary>
    public static GrayImage Make(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(439);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        return SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
    }
}
