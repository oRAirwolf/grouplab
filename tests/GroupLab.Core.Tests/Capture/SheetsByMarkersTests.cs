using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 281: a picture whose codes cannot be read is named by its markers' layout rather than refused, as three of the
/// six pictures of the Fold 7's camera test were. Where variants share a layout every one is offered, so the person chooses among those.
/// </summary>
public class SheetsByMarkersTests
{
    [Fact]
    public void ASheetIsNamedByItsMarkersWithoutItsCodes()
    {
        var library = BuiltIns.Files.Select(BuiltIns.Load).ToList();
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        // Naming by markers never looks at the codes, so the sheet is rendered plainly; its markers get about 24 pixels a side at 150 dpi.
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 150);
        var backend = new OpenCvSharpBackend();

        var named = LiveSheet.SheetsByMarkers(render, library, backend);
        Assert.Contains(named, d => d.Name == definition.Name);
        var layout = definition.Fiducials!.Markers!.Select(m => (m.Id, m.X, m.Y)).ToHashSet();
        Assert.All(named, d => Assert.Subset(d.Fiducials!.Markers!.Select(m => (m.Id, m.X, m.Y)).ToHashSet(), layout));
    }

    [Fact]
    public void AmongSheetsSharingALayoutTheOneDrawnIsChosen()
    {
        var library = BuiltIns.Files.Select(BuiltIns.Load).ToList();
        var backend = new OpenCvSharpBackend();
        var fiveByFive = library.Where(d => d.Name.StartsWith("GroupLab 5x5 Load Development", StringComparison.Ordinal)).ToList();
        Assert.True(fiveByFive.Count(d => d.Name.Contains("Letter", StringComparison.Ordinal)) >= 6, "the Letter 5x5 sheets and their variants share one layout");
        foreach (var definition in fiveByFive)
        {
            var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 100);
            var sameLayout = LiveSheet.SheetsByMarkers(render, library, backend);
            Assert.Contains(sameLayout, d => d.Name == definition.Name);
            Assert.Equal(definition.Name, LiveSheet.MostAlike(render, sameLayout, backend)?.Name);
        }
    }

    /// <summary>
    /// Entry 282 section 5: each code cut out where the markers put it, and read enlarged. At 100 dpi a module is about 1.6 pixels, too few to
    /// read as it stands, and a cut-out read at up to four times holds it.
    /// </summary>
    [Fact]
    public void EachCodeIsCutOutWhereTheMarkersPutItAndReadEnlarged()
    {
        var library = BuiltIns.Files.Select(BuiltIns.Load).ToList();
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 100);
        var backend = new OpenCvSharpBackend();
        var crops = LiveSheet.CodeCrops(render, library, backend);
        Assert.Equal(definition.Codes!.Positions.Count, crops.Count);
        Assert.All(crops, crop => Assert.Contains(GroupLab.Core.Registration.SheetIdentification.CropScales, s => backend.ReadCodes(crop, s).Count > 0));
        var identity = GroupLab.Core.Registration.SheetIdentification.Identify(render, library, backend, new GroupLab.Core.Trace.TraceRecorder());
        Assert.Equal(definition.Name, identity.Definition?.Name);
    }
}
