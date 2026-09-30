using System.Xml.Linq;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 300 section 5: a bull or a grid on the site is served as vectors from the same scene as its PDF, one path an ink
/// run, so it is sharp at any size and small.
/// </summary>
public class SceneSvgTests
{
    [Fact]
    public void APageIsValidSvgWithOnePathAnInkRunAndItsColors()
    {
        var page = SceneBuilder.Build(BuiltIns.Load("GL-CF25-LTR.gltd.json"), new RenderOptions(BullColour: BullColour.Red)).Pages[0];
        var svg = XDocument.Parse(SceneSvg.Write(page));
        XNamespace ns = "http://www.w3.org/2000/svg";
        var paths = svg.Root!.Elements(ns + "path").ToList();
        Assert.InRange(paths.Count, 2, page.Items.Count / 10);
        Assert.Contains(paths, p => (string?)p.Attribute("fill") == "#D22630");
        Assert.Contains(paths, p => (string?)p.Attribute("fill") == "#000000");
    }

    [Fact]
    public void ACropKeepsOnlyWhatReachesIntoIt()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR-C.gltd.json");
        var page = SceneBuilder.Build(definition).Pages[0];
        var bull = definition.Bulls.First(b => b.Scoring);
        double reach = 0.6 * definition.RingSets.First(r => r.Key == bull.RingSet).Discs[0].Diameter;
        string whole = SceneSvg.Write(page), crop = SceneSvg.Write(page, bull.X - reach, bull.Y - reach, bull.X + reach, bull.Y + reach);
        Assert.True(crop.Length * 20 < whole.Length, $"the crop is {crop.Length} characters against the page's {whole.Length}");
        Assert.Contains("viewBox", crop, StringComparison.Ordinal);
    }
}
