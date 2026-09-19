using GroupLab.Core.Reporting;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 113 section 6: docs/USER-GUIDE.md is illustrated with the committed renders, and every screenshot it names
/// exists, so the guide and the renders cannot drift apart unnoticed; its PDF is committed beside it; and the document writer places pictures.
/// </summary>
public class UserGuideTests
{
    [Fact]
    public void EveryScreenshotTheGuideNamesExists()
    {
        string guide = File.ReadAllText(Repo.PathTo("docs", "USER-GUIDE.md"));
        var pictures = DocumentPdf.Pictures(guide);
        Assert.True(pictures.Count >= 8, $"{pictures.Count} pictures");
        Assert.All(pictures, p =>
        {
            Assert.StartsWith("figures/screens/current/", p, StringComparison.Ordinal);
            Assert.True(File.Exists(Repo.PathTo(["docs", .. p.Split('/')])), $"docs/{p} is named by the guide and is not there");
        });
        Assert.True(File.Exists(Repo.PathTo("docs", "USER-GUIDE.pdf")), "docs/USER-GUIDE.pdf is missing: run `grouplab user-guide` and commit it.");
    }

    [Fact]
    public void TheDocumentWriterSetsTextAndPictures()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xD9];
        var pages = DocumentPdf.Pages("# Title\n\nA paragraph with `code`, **bold** and [a link](https://example.org).\n\n![A picture](p.png)\n\n1. First\n- A bullet", _ => new DocumentImage(jpeg, 400, 200));
        var text = string.Join(" ", pages.SelectMany(p => p.Items.OfType<GroupLab.Core.Rendering.TextRun>()).Select(t => t.Text));
        Assert.Contains("A paragraph with code, bold and a link (https://example.org).", text, StringComparison.Ordinal);
        Assert.Contains("A picture", text, StringComparison.Ordinal);
        var image = Assert.Single(pages.SelectMany(p => p.Items.OfType<GroupLab.Core.Rendering.ImageBox>()));
        Assert.Equal(image.Width / 2, image.Height);
        string pdf = System.Text.Encoding.Latin1.GetString(DocumentPdf.Write("![x](p.png)", _ => new DocumentImage(jpeg, 400, 200)));
        Assert.Contains("/Subtype /Image /Width 400 /Height 200", pdf, StringComparison.Ordinal);
        Assert.Contains("/XObject << /Im0 ", pdf, StringComparison.Ordinal);
        Assert.Contains("/Im0 Do", pdf, StringComparison.Ordinal);
    }
}
