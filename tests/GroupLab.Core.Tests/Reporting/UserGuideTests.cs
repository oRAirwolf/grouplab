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
    public void EveryScreenshotTheGuidesNameExists()
    {
        // Entry 116 section 5: the tester's page ships the same way, so both are held to their pictures and to their PDFs.
        foreach (string guide in new[] { "USER-GUIDE", "TESTING-GUIDE" })
        {
            var pictures = DocumentPdf.Pictures(File.ReadAllText(Repo.PathTo("docs", guide + ".md")));
            Assert.True(pictures.Count >= 1, $"{guide}.md names {pictures.Count} pictures");
            Assert.All(pictures, p =>
            {
                // Entry 437 section 1.1: section 13 shows the phone's own pictures.
                Assert.True(p.StartsWith("figures/screens/current/", StringComparison.Ordinal) || p.StartsWith("figures/screens/phone/", StringComparison.Ordinal), p);
                Assert.True(File.Exists(Repo.PathTo(["docs", .. p.Split('/')])), $"docs/{p} is named by {guide}.md and is not there");
            });
            Assert.True(File.Exists(Repo.PathTo("docs", guide + ".pdf")), $"docs/{guide}.pdf is missing: run `grouplab user-guide` and commit it.");
        }

        // Entry 155: the target library and the print screen became one screen, Targets, so the guide shows one picture where it showed two.
        Assert.True(DocumentPdf.Pictures(File.ReadAllText(Repo.PathTo("docs", "USER-GUIDE.md"))).Count >= 7);
    }

    /// <summary>Entry 235: a page taken to the range is one page, and its PDF is committed beside it.</summary>
    [Fact]
    public void EveryPrintableIsOnePageAndHasItsPdf()
    {
        foreach (string page in GroupLab.Cli.GuideVerb.Printables)
        {
            var pages = DocumentPdf.Pages(File.ReadAllText(Repo.PathTo("docs", page + ".md")), _ => null);
            Assert.True(pages.Count == 1, $"{page}.md prints on {pages.Count} pages");
            Assert.True(File.Exists(Repo.PathTo("docs", page + ".pdf")), $"docs/{page}.pdf is missing: run `grouplab user-guide` and commit it.");
        }
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

    /// <summary>Entry 436 section 4.2: the guide's PDF has a bookmark for every section and subsection, each subsection under its section.</summary>
    [Fact]
    public void TheGuidePdfHasABookmarkForEverySectionAndSubsection()
    {
        string markdown = File.ReadAllText(Repo.PathTo("docs", "USER-GUIDE.md"));
        var headings = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(l => l.StartsWith("## ", StringComparison.Ordinal) || l.StartsWith("### ", StringComparison.Ordinal)).ToList();
        var bookmarks = DocumentPdf.Bookmarks(markdown);
        Assert.Equal(headings.Select(h => DocumentPdf.Inline(h.TrimStart('#').Trim())), bookmarks.Select(b => b.Title));
        Assert.Contains(bookmarks, b => b.Level == 3);

        string pdf = System.Text.Encoding.Latin1.GetString(DocumentPdf.Write("# T\n\n## One\n\n### A\n\nText.\n\n### B\n\n## Two\n", _ => null));
        Assert.Contains("/Outlines ", pdf, StringComparison.Ordinal);
        Assert.Contains("/PageMode /UseOutlines", pdf, StringComparison.Ordinal);
        Assert.Contains("/Title (One)", pdf, StringComparison.Ordinal);
        Assert.Contains("/Count -2", pdf, StringComparison.Ordinal);
        Assert.Contains("/Type /Outlines /First 7 0 R /Last 10 0 R /Count 2", pdf, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 437 section 3.5: in the PDF a marked picture carries its markings, drawn from the same positions file the site reads, and the
    /// marks line under a picture is a note for the site, never printed as text.
    /// </summary>
    [Fact]
    public void TheGuidePdfDrawsEachMarkingOnItsControl()
    {
        string markdown = File.ReadAllText(Repo.PathTo("docs", "USER-GUIDE.md"));
        var marks = GroupLab.Cli.GuideVerb.Marks(markdown);
        Assert.NotEmpty(marks);
        var (path, names) = marks.First();
        string png = Repo.PathTo(["docs", .. path.Split('/')]);
        var plain = GroupLab.Cli.GuideVerb.Picture(png)!;
        var marked = GroupLab.Cli.GuideVerb.Picture(png, names)!;
        Assert.NotEqual(plain.Jpeg, marked.Jpeg);

        using var bounds = System.Text.Json.JsonDocument.Parse(File.ReadAllText(png.Replace("-light-1400x900.png", "-bounds.json", StringComparison.Ordinal)));
        var box = bounds.RootElement.GetProperty("controls").GetProperty(names[0]);
        using var image = OpenCvSharp.Cv2.ImDecode(marked.Jpeg, OpenCvSharp.ImreadModes.Color);
        // Halfway along the outline's top edge, the amber line (blue low, red high) is drawn.
        var at = image.At<OpenCvSharp.Vec3b>(box[1].GetInt32(), box[0].GetInt32() + (box[2].GetInt32() / 2));
        Assert.True(at.Item2 > 150 && at.Item0 < 120, $"the outline of {names[0]} is not amber at its top edge: {at.Item2},{at.Item1},{at.Item0}");

        var text = string.Join(" ", DocumentPdf.Pages(markdown, _ => null).SelectMany(p => p.Items.OfType<GroupLab.Core.Rendering.TextRun>()).Select(t => t.Text));
        Assert.DoesNotContain("marks:", text, StringComparison.Ordinal);
    }

    /// <summary>Every other PDF GroupLab writes, sheets above all, is byte for byte what it was without bookmarks.</summary>
    [Fact]
    public void APdfWithNoBookmarksIsUnchanged()
    {
        var pages = DocumentPdf.Pages("# T\n\nText.", _ => null);
        Assert.Equal(GroupLab.Core.Rendering.Pdf.PdfWriter.Write(pages), GroupLab.Core.Rendering.Pdf.PdfWriter.Write(pages, []));
        Assert.DoesNotContain("/Outlines", System.Text.Encoding.Latin1.GetString(GroupLab.Core.Rendering.Pdf.PdfWriter.Write(pages)), StringComparison.Ordinal);
    }
}
