using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 3.3: the "looks like a GroupLab sheet" check reuses where identification's full-size reading
/// found the codes, rather than searching the same picture again with the same detectors (8.4 s to 0.15 s on a photo with none).
/// </summary>
public class Entry363LookTests
{
    [Fact]
    public void TheCodesLocatedAfterAReadingAreTheOnesASearchFinds()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "targets", "GL-CF25-LTR.gltd.json");
        var definition = GltdJsonReader.Read(File.ReadAllBytes(file)).Definition!;
        var page = SceneBuilder.Build(definition).Pages[0];
        var read = SceneRasterizer.Rasterize(page, 300);
        var fresh = SceneRasterizer.Rasterize(page, 300);
        var backend = new OpenCvSharpBackend();

        Assert.NotEmpty(backend.ReadCodes(read, 1.0));
        var kept = backend.LocateCodes(read);
        var searched = backend.LocateCodes(fresh);
        Assert.Equal(searched.Count, kept.Count);
        Assert.NotEmpty(kept);
        Assert.All(kept, box => Assert.Contains(searched, other => Math.Abs(other.Average(p => p.X) - box.Average(p => p.X)) < 1 && Math.Abs(other.Average(p => p.Y) - box.Average(p => p.Y)) < 1));
    }
}
