using System.Text;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 25 section 2 point 5: every PDF asks its viewer for no scaling, and the print note along the bottom
/// edge is clear of everything else on every page of every built-in sheet, while a render without it is unchanged.
/// </summary>
public class PrintNoteTests
{
    public static TheoryData<string> Files => [.. BuiltIns.Files];

    [Theory]
    [MemberData(nameof(Files))]
    public void ThePrintNoteIsClearOfEverythingAndChangesNothingElse(string file)
    {
        var definition = BuiltIns.Load(file);
        var plain = SceneBuilder.Build(definition);
        var noted = SceneBuilder.Build(definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote));
        Assert.Equal(plain.Pages.Count, noted.Pages.Count);
        for (int p = 0; p < noted.Pages.Count; p++)
        {
            var page = noted.Pages[p];
            Assert.DoesNotContain(plain.Pages[p].Items, i => i.Layer == SceneLayer.PrintNote);
            Assert.Equal(plain.Pages[p].Items, page.Items.Where(i => i.Layer != SceneLayer.PrintNote));

            var note = Assert.Single(page.Items.OfType<TextRun>(), t => t.Layer == SceneLayer.PrintNote);
            Assert.Equal(SceneBuilder.PrintNoteFontSize, note.FontSize);
            var box = Box(note);
            Assert.True(page.Height - box.Y1 >= 2 * 36, $"{file}: the note comes within {(page.Height - box.Y1) / 2:0} dmm of the bottom edge");
            Assert.True(box.X0 >= 600 && box.X1 <= page.Width - 600, $"{file}: the note runs into the side margins");
            foreach (var item in page.Items.Where(i => i.Layer != SceneLayer.PrintNote))
            {
                var other = Box(item);
                double gap = Math.Max(Math.Max(other.X0 - box.X1, box.X0 - other.X1), Math.Max(other.Y0 - box.Y1, box.Y0 - other.Y1));
                Assert.True(gap >= 2 * 8, $"{file} page {p}: the note is {gap / 2:0.0} dmm from a {item.Layer} item");
            }
        }

        string pdf = Encoding.Latin1.GetString(TargetRenderer.Render(definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pdf!);
        Assert.Contains("/ViewerPreferences << /PrintScaling /None >>", pdf, StringComparison.Ordinal);
        Assert.Contains("Print at actual size", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLibraryListsEveryBuiltInByWhatItIsFor()
    {
        var sheets = TargetLibrary.Load(Repo.PathTo("targets"));
        Assert.Equal(BuiltIns.Files.Count(), sheets.Count);
        Assert.Equal(22, sheets.Count);
        Assert.All(sheets, s =>
        {
            Assert.NotEqual(TargetLibrary.OtherFamily, s.Family);
            Assert.NotNull(s.DesignedFor);
            Assert.False(string.IsNullOrWhiteSpace(s.Definition.Name));
        });

        var tiled = sheets.Single(s => s.File == "GL-LR300-T.gltd.json");
        Assert.Equal(4, tiled.Sheets);
        Assert.Contains("300 yd", tiled.Summary, StringComparison.Ordinal);
        Assert.Contains("4 sheets", tiled.Summary, StringComparison.Ordinal);
        Assert.Contains("Letter, 215.9 by 279.4 mm", sheets.Single(s => s.File == "GL-CF25-LTR.gltd.json").Summary, StringComparison.Ordinal);
        Assert.Equal("Centerfire load development", sheets[0].Family);
    }

    private static (double X0, double Y0, double X1, double Y1) Box(SceneItem item) => item switch
    {
        DiscBand d => (d.CentreX - d.OuterRadius, d.CentreY - d.OuterRadius, d.CentreX + d.OuterRadius, d.CentreY + d.OuterRadius),
        RectFill r => (r.X, r.Y, r.X + r.Width, r.Y + r.Height),
        TextRun t => TextBox(t),
        _ => throw new InvalidOperationException(item.GetType().Name),
    };

    /// <summary>A generous box for a line of text: the full advance width, three quarters of the size above the baseline and a quarter below.</summary>
    private static (double X0, double Y0, double X1, double Y1) TextBox(TextRun t)
    {
        double width = HelveticaMetrics.TextWidth(t.Text, t.FontSize);
        double x0 = t.Anchor switch
        {
            TextAnchor.Centre => t.X - (width / 2),
            TextAnchor.Right => t.X - width,
            _ => t.X,
        };
        return (x0, t.Baseline - (0.75 * t.FontSize), x0 + width, t.Baseline + (0.25 * t.FontSize));
    }
}
