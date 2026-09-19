using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 106 section 4: the list of every calibre and cartridge the input knows is generated from the code, and the
/// committed copy is held to it, the way the gate record's tables are held to theirs, so the list cannot drift from what GroupLab does.
/// </summary>
public class CalibreListTests
{
    [Fact]
    public void TheCommittedListIsWhatTheCommandWrites()
    {
        string committed = File.ReadAllText(Repo.PathTo("docs", "CALIBRES.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.True(committed == CalibreList.Markdown(), "docs/CALIBRES.md differs from what the code knows. Run `grouplab calibres` and commit docs/CALIBRES.md and docs/CALIBRES.pdf.");
        Assert.True(File.Exists(Repo.PathTo("docs", "CALIBRES.pdf")));
    }

    /// <summary>Every pick-list diameter is in the list in both units, and so is the refusal's sentence.</summary>
    [Fact]
    public void TheListCarriesEveryDiameterAndTheRule()
    {
        string list = CalibreList.Markdown();
        Assert.All(Calibre.Diameters, d => Assert.Contains(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"| {d.ToString(".000#", System.Globalization.CultureInfo.InvariantCulture)} | {d * 25.4:0.00} |"), list, StringComparison.Ordinal));
        Assert.Contains(Calibre.Refusal, list, StringComparison.Ordinal);
        Assert.DoesNotContain("\u2014", list, StringComparison.Ordinal);
    }

    /// <summary>The PDF's every line of text and every rule lies inside the page's margins, wrapped where a table is wider than the page.</summary>
    [Fact]
    public void ThePdfKeepsEveryLineOnThePage()
    {
        var pages = CalibreList.Pages(CalibreList.Markdown());
        Assert.InRange(pages.Count, 1, 6);
        foreach (var page in pages)
        {
            Assert.Equal((4318, 5588), (page.Width, page.Height));
            foreach (var item in page.Items)
            {
                (long left, long right, long top, long bottom) = item switch
                {
                    TextRun t => (t.X, t.X + HelveticaMetrics.TextWidth(t.Text, t.FontSize), t.Baseline - t.FontSize, t.Baseline),
                    RectFill r => (r.X, r.X + r.Width, r.Y, r.Y + r.Height),
                    _ => (0, 0, 0, 0),
                };
                Assert.True(left >= 360 && right <= page.Width - 360 && top >= 0 && bottom <= page.Height - 360, $"{item} runs off the page");
            }
        }
    }
}
