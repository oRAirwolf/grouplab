using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Reporting;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112 section 2: the report's layout, from GroupLab's own PDF writer. A report longer than two pages continues
/// rather than being cut, with the table's headings repeated; and no character a report carries is lost to WinAnsiEncoding.
/// </summary>
public class ReportWriterTests
{
    private static SessionReport Report(int shots) => new(
        "Test sheet",
        [new("Sheet", "Test sheet"), new("Date", "2026-09-19")],
        new ReportPlot([new ReportDisc(2, new Rgb(0, 0, 0), false), new ReportDisc(1.8, new Rgb(255, 255, 255), true)],
            [.. Enumerable.Range(1, shots).Select(i => new ReportShot(i.ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(Math.Cos(i) * 0.3, Math.Sin(i) * 0.3), i == 3))],
            0.308, new PointD(0.01, -0.02), 0.2, 0.4, "caption"),
        ["summary"],
        [new ReportFigure("Mean radius", "0.132 in", ["94.9% interval 0.110 to 0.166 in", "without exclusions: 0.127 in"])],
        new ReportCard("Zero correction", "Not distinguishable from zero at 23 shots.", ["give or take"], ["why"]),
        [new ReportCard("Shape", "Round, as far as 24 shots can tell.", ["p = 0.549"], [])],
        ["shot", "bull", "standing"],
        [.. Enumerable.Range(1, shots).Select(i => new ReportRow([$"shot {i}", "1", i == 3 ? "excluded: pulled shot" : "counted"], i == 3))],
        ["Shot 3: Pulled shot."],
        [],
        ["Registered."],
        [new ReportSection("Zero correction", ["why"])],
        [new ReportPair("GroupLab", "1.0.0")]);

    private static IEnumerable<string> Texts(Scene page) => page.Items.OfType<TextRun>().Select(t => t.Text);

    [Fact]
    public void ALongReportContinuesOnFurtherPagesWithItsHeadingsRepeated()
    {
        var pages = ReportWriter.Pages(Report(120));
        Assert.True(pages.Count >= 4, $"{pages.Count} pages");
        var all = pages.SelectMany(Texts).ToList();
        Assert.All(Enumerable.Range(1, 120), i => Assert.Contains($"shot {i}", all));
        Assert.All(pages.Skip(2).Where(p => Texts(p).Any(t => t.StartsWith("shot ", StringComparison.Ordinal))), p => Assert.Contains("standing", Texts(p)));
        Assert.Contains($"page {pages.Count} of {pages.Count}", Texts(pages[^1]));
        Assert.All(pages, p => Assert.All(p.Items.OfType<TextRun>(), t => Assert.InRange(t.Baseline, 0, p.Height)));

        byte[] pdf = ReportWriter.Write(Report(5));
        Assert.Equal(2, PDFtoImage.Conversion.GetPageCount(pdf));
    }

    [Fact]
    public void TheCharactersWinAnsiLacksAreWrittenInWords()
    {
        Assert.Equal("sigma <= 0.1 - about 2 > ...", ReportWriter.Plain("\u03c3 \u2264 0.1 \u2212 \u2248 2 \u203a \u2026"));
        Assert.True(ReportWriter.Printable("0.132 \u00d7 0.314 in \u00b7 CEP 50, 95.0% interval, \u00b1 1\u00b0"));
        Assert.False(ReportWriter.Printable("\u4e2d"));
        Assert.DoesNotContain("?", PdfWriter.Content(ReportWriter.Pages(Report(5))[0]), StringComparison.Ordinal);
    }
}
