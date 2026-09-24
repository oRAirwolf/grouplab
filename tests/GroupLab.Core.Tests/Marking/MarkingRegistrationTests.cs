using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112 section 1: a session reopens and reads with no image, so a marking keeps its sheet's registration as the
/// numbers each model is built from, and a marking read back maps every point where the original did. A file from before, with no mapping,
/// still reads, with its note.
/// </summary>
public class MarkingRegistrationTests
{
    private static MarkingState With(ScaleReference scale)
    {
        var session = new MarkingSession();
        session.Open("sheet.png", 1);
        session.SetScale(scale);
        session.AddShot(new PointD(1200, 900));
        return session.State;
    }

    private static void SameMapping(IPageMapping original, IPageMapping read)
    {
        foreach (var at in new[] { new PointD(100, 120), new PointD(1500, 1100), new PointD(3200, 4100) })
        {
            var a = original.ToPage(at);
            var b = read.ToPage(at);
            Assert.True(Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9, $"{at}: {a} against {b}");
        }
    }

    [Fact]
    public void EachRegistrationModelSurvivesTheMarkingFile()
    {
        var homography = new HomographyMapping(new Homography([0.085, 0.001, 3, -0.0008, 0.0849, 5, 1e-7, -2e-7, 1]));
        var radial = new RadialHomographyMapping(2000, 1500, 2000, -0.03, 0.004, new Homography([1100, 12, 1080, -9, 1098, 1400, 0.001, -0.002, 1]));
        var surface = new SurfaceMapping(SyntheticSurface.Camera(4000, 3000, 2600, -0.05, 0.065, 2600, 8 * Math.PI / 180, 2159, 2794, 0.4, [0.3, 0.25, -0.2, 0.15]), 0, 0, 2159, 2794);
        foreach (IPageMapping mapping in new IPageMapping[] { homography, radial, surface })
        {
            var sheet = new SheetReference(mapping, "38 of 38 markers found") { MarkersFound = 38, MarkersExpected = 38 };
            var (state, notes) = MarkingFile.Read(MarkingFile.Write(With(sheet)));
            var read = Assert.IsType<SheetReference>(state.Scale);
            Assert.Empty(notes);
            Assert.Equal(mapping.Model, read.Mapping.Model);
            Assert.Equal(38, read.MarkersFound);
            Assert.Equal("38 of 38 markers found", read.Summary);
            SameMapping(mapping, read.Mapping);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 171 section 1.4: the file says whether its figures are real inches, corrected by a scan's print scale, or
    /// the sheet's own, so a saved group from a scan and one from a photograph can be told apart. A file from before entry 171 has neither and
    /// reads as the sheet's own inches, which is what it was.
    /// </summary>
    [Fact]
    public void TheFileSaysWhetherItsFiguresAreRealInches()
    {
        var mapping = new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1]));
        foreach (double? scale in new double?[] { 0.962, null })
        {
            string json = MarkingFile.Write(With(new SheetReference(mapping, "s") { PrintScale = scale }));
            Assert.Equal(scale is null ? "sheet" : "real", (string?)System.Text.Json.Nodes.JsonNode.Parse(json)!["scale"]!["inches"]);
            var read = Assert.IsType<SheetReference>(MarkingFile.Read(json).State.Scale);
            Assert.Equal(scale, read.PrintScale);
            Assert.Equal(254 * (scale ?? 1) / 254, read.ToTarget(new PointD(254, 0)).X, 12);
        }
    }

    [Fact]
    public void AMarkingWithoutItsRegistrationStillReadsWithItsNote()
    {
        string json = MarkingFile.Write(With(new SheetReference(new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1])), "s")));
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        node["scale"]!.AsObject().Remove("mapping");
        var (state, notes) = MarkingFile.Read(node.ToJsonString());
        Assert.Null(state.Scale);
        Assert.Contains(notes, n => n.Contains("Detect on the GroupLab sheet again", StringComparison.Ordinal));
    }
}
