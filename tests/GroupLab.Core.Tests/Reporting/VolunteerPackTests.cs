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
            "actual size", "bull 1 to the centre of bull 5", "5.98 in (152.0 mm)", "Flat", "One shot per bull, in order", "Write nothing on this sheet",
            "Guided", "Manual", "torch", "score", "2.5 ft", "main camera", "uncropped", "messaging app", "600 dpi", "https://grouplab.org/targets", "terms on that page",
        })
        {
            Assert.Contains(expected, text, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("consent", text, StringComparison.OrdinalIgnoreCase);

        // Entry 264: a zeroing grid is checked by its bar and shot at its diamond; a sheet with a load block is written on only there.
        var grid = BuiltIns.Load("GL-ZERO-MOA-100Y.gltd.json");
        string zero = VolunteerPack.Filled(VolunteerPack.Source(), grid);
        Assert.Contains("Measure the bar under the grid", zero, StringComparison.Ordinal);
        Assert.Contains("Shoot one group at the diamond", zero, StringComparison.Ordinal);
        Assert.DoesNotContain("bull 5", zero, StringComparison.Ordinal);
        Assert.Contains("only in the load block", VolunteerPack.Filled(VolunteerPack.Source(), BuiltIns.Load("GL-CF25-LTR-D-E.gltd.json")), StringComparison.Ordinal);
        var gridRender = TargetRenderer.Render(grid, new RenderOptions());
        _ = VolunteerPack.Page(grid, gridRender.Pages[0].Width, gridRender.Pages[0].Height);
        Assert.All(page.Items.OfType<TextRun>(), t => Assert.True(ReportWriter.Printable(t.Text), t.Text));

        byte[] pdf = VolunteerPack.Write(definition, render.Pages);
        Assert.Equal(render.Pages.Count + 1, PDFtoImage.Conversion.GetPageCount(pdf));
        Assert.Throws<InvalidOperationException>(() => VolunteerPack.Page(definition, render.Pages[0].Width, render.Pages[0].Height, string.Concat(Enumerable.Repeat("- a line of instructions that goes on\n", 200))));
    }
}
