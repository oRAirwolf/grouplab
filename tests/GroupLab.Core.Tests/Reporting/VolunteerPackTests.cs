using GroupLab.Core.Rendering;
using GroupLab.Core.Reporting;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 113 section 5: the volunteer pack. One page of instructions generated from docs/VOLUNTEER-PACK.md, which is
/// embedded as it stands, carrying everything the entry lists, the sheet's own distance from bull 1 to bull 5, and no consent form.
/// </summary>
public class VolunteerPackTests
{
    [Fact]
    public void ThePackIsTheSheetAndOnePageCarryingEveryInstruction()
    {
        Assert.Equal(File.ReadAllText(Repo.PathTo("docs", "VOLUNTEER-PACK.md")).Replace("\r\n", "\n", StringComparison.Ordinal), VolunteerPack.Source().Replace("\r\n", "\n", StringComparison.Ordinal));
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        Assert.Equal("5.98 in (152.0 mm)", VolunteerPack.BullOneToFive(definition));

        var render = TargetRenderer.Render(definition, new RenderOptions());
        var page = VolunteerPack.Page(definition, render.Pages[0].Width, render.Pages[0].Height);
        Assert.Equal((render.Pages[0].Width, render.Pages[0].Height), (page.Width, page.Height));
        string text = string.Join(" ", page.Items.OfType<TextRun>().Select(t => t.Text));
        foreach (string expected in new[]
        {
            "actual size", "bull 1 to the centre of bull 5", "5.98 in (152.0 mm)", "flat", "One shot per bull, in order", "Write only in the load block",
            "four photographs", "2.5 ft", "main camera", "Do not crop", "messaging app", "600 dpi", "https://pissinhot.com/targets", "terms on that page",
        })
        {
            Assert.Contains(expected, text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("consent", text, StringComparison.OrdinalIgnoreCase);
        Assert.All(page.Items.OfType<TextRun>(), t => Assert.True(ReportWriter.Printable(t.Text), t.Text));

        byte[] pdf = VolunteerPack.Write(definition, render.Pages);
        Assert.Equal(render.Pages.Count + 1, PDFtoImage.Conversion.GetPageCount(pdf));
        Assert.Throws<InvalidOperationException>(() => VolunteerPack.Page(definition, render.Pages[0].Width, render.Pages[0].Height, string.Concat(Enumerable.Repeat("- a line of instructions that goes on\n", 200))));
    }
}
