using System.Collections.Immutable;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// Suggestion 61 (Discord, Alan's yes on 2026-11-24): Compare loads exported as a PDF and as a picture of the same page, with a modest footer
/// naming GroupLab and grouplab.org and no watermark. The page carries each load's group, its figures with their intervals in full, what the
/// difference means and the tests; it fits on one page for a few loads and takes a second in the PDF where it must, while the picture is
/// always one image.
/// </summary>
public class ComparisonPageTests
{
    private static List<PointD> Group(int seed, double spread, int shots = 10)
    {
        var random = new Random(seed);
        return [.. Enumerable.Range(0, shots).Select(_ => new PointD(spread * ((2 * random.NextDouble()) - 1), spread * ((2 * random.NextDouble()) - 1)))];
    }

    private static ComparisonPage Page(int loads, PageSize paper = PageSize.Letter)
    {
        var groups = Enumerable.Range(0, loads).Select(i => ($"Test load {i + 1}", (IReadOnlyList<PointD>)Group(i, 0.3 + (0.1 * i)))).ToList();
        var report = LoadComparison.Compare(groups, UnitSettings.Imperial.Length);
        var exported = groups.Select((g, i) => new ExportedLoad(g.Item1, "10 shots, 1 session, 2026-11-23", i == 0 ? "2810 ft/s" : null)).ToList();
        return ComparisonPages.For(report, exported, $"{loads} loads, {10 * loads} shots, in the order chosen.", [], UnitSettings.Imperial, 1800, paper,
            "2026-11-24", "1.0.0-nightly.244");
    }

    private static List<string> Texts(Scene page) => [.. page.Items.OfType<TextRun>().Select(t => t.Text)];

    [Fact]
    public void TwoLoadsAreOnePageWithEverySectionAndAModestFooter()
    {
        var page = Page(2);
        var scene = Assert.Single(ComparisonPages.Pages(page));
        var texts = Texts(scene);
        Assert.Equal(2159 * 2, scene.Width);
        Assert.Equal(2794 * 2, scene.Height);
        foreach (string words in new[] { "Compare loads", "GroupLab comparison, dated 2026-11-24", "Figures, each with its 95 percent interval", "What the difference means", "Tests run" })
        {
            Assert.Contains(words, texts);
        }

        Assert.Contains("Test load 1", texts);
        Assert.Contains("Test load 2", texts);
        Assert.Contains(page.Report.Headline, texts);
        Assert.Contains(texts, t => t.Contains("2810 ft/s", StringComparison.Ordinal));

        // Each interval in full, under its figure, never cut short.
        foreach (var row in page.Rows)
        {
            Assert.Contains(row.SigmaInterval, texts);
            Assert.Contains(row.MeanRadiusInterval, texts);
            Assert.StartsWith("95% interval ", row.SigmaInterval, StringComparison.Ordinal);
            Assert.EndsWith(" MOA)", row.MeanRadius, StringComparison.Ordinal);
        }

        // The footer: GroupLab and grouplab.org, small and grey at the foot, and nothing written across the page.
        var footer = scene.Items.OfType<TextRun>().Single(t => t.Text.Contains(ComparisonPages.Branding, StringComparison.Ordinal));
        Assert.Equal("Made with GroupLab 1.0.0-nightly.244, grouplab.org", footer.Text);
        Assert.True(footer.Baseline > scene.Height * 0.95, "the footer sits in the bottom margin");
        Assert.True(footer.FontSize <= scene.Items.OfType<TextRun>().Max(t => t.FontSize) / 2, "the footer is small beside the title");
        Assert.DoesNotContain(scene.Items.OfType<TextRun>(), t => t.Text.Contains("GroupLab", StringComparison.Ordinal) && t != footer
            && !t.Text.StartsWith("GroupLab comparison", StringComparison.Ordinal));
        Assert.All(scene.Items.OfType<TextRun>(), t => Assert.True(ReportWriter.Printable(t.Text), t.Text));
        Assert.All(scene.Items.OfType<TextRun>(), t => Assert.InRange(t.Baseline, 0, scene.Height));
    }

    [Fact]
    public void ManyLoadsTakeASecondPageInThePdfAndOneTallPicture()
    {
        var page = Page(5);
        var pages = ComparisonPages.Pages(page);
        Assert.Equal(2, pages.Count);
        Assert.All(pages, p => Assert.Contains(p.Items.OfType<TextRun>(), t => t.Text.EndsWith(ComparisonPages.Branding, StringComparison.Ordinal)));
        Assert.Equal(Texts(ComparisonPages.Tall(page)).Count(t => t != page.Footer && !t.StartsWith("Compare loads, ", StringComparison.Ordinal)),
            pages.Sum(p => Texts(p).Count(t => t != page.Footer && !t.StartsWith("Compare loads, ", StringComparison.Ordinal))));
        Assert.Contains("Details: every pair's sigma ratio", pages.SelectMany(Texts));

        byte[] pdf = ComparisonPages.Write(page);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void ThePictureIsThePageInItsLoadsColors()
    {
        var page = Page(2);
        byte[] png = ComparisonPages.Picture(page);
        using var picture = Cv2.ImDecode(png, ImreadModes.Color);
        Assert.False(picture.Empty());
        Assert.Equal((int)Math.Round(215.9 / 25.4 * ComparisonPages.PictureDpi), picture.Width);
        Assert.Equal(ComparisonPages.PictureDpi, GroupLab.Core.Printing.Thermal.ThermalPng.Dpi(png)!.Value, 0);

        // Each load's own color is on the picture, as the screen draws it.
        for (int i = 0; i < 2; i++)
        {
            var ink = ComparisonPages.LoadInk(i);
            using var mask = new Mat();
            Cv2.InRange(picture, new Scalar(ink.B - 12, ink.G - 12, ink.R - 12), new Scalar(ink.B + 12, ink.G + 12, ink.R + 12), mask);
            Assert.True(Cv2.CountNonZero(mask) > 500, $"load {i + 1}'s color");
        }
    }

    [Fact]
    public void ColourPngKeepsEveryPixel()
    {
        byte[] bgr = [.. Enumerable.Range(0, 7 * 5 * 3).Select(i => (byte)(i * 7))];
        using var decoded = Cv2.ImDecode(ColourPng.Write(7, 5, bgr, 96), ImreadModes.Color);
        Assert.Equal(7, decoded.Width);
        Assert.Equal(5, decoded.Height);
        decoded.GetArray(out Vec3b[] pixels);
        Assert.Equal(bgr, pixels.SelectMany(p => new[] { p.Item0, p.Item1, p.Item2 }));
    }

    [Fact]
    public void ALoadsSpeedIsWordedAsTheScreenWordsIt()
    {
        var book = RecordBook.Empty with
        {
            Loads = ImmutableList.Create(
                new Load("Measured", null) { MuzzleVelocityFps = 2810, MuzzleVelocitySdFps = 9.4, MuzzleVelocitySdFrom = "12 readings" },
                new Load("Typed", null) { MuzzleVelocityFps = 2700 }),
        };
        Assert.Equal("2810 ft/s, SD 9 ft/s from 12 readings", ComparisonPages.Speed(book, "Measured", UnitSettings.Imperial));
        Assert.Equal("2700 ft/s", ComparisonPages.Speed(book, "Typed", UnitSettings.Imperial));
        Assert.Null(ComparisonPages.Speed(book, "Not in the book", UnitSettings.Imperial));
        Assert.Null(ComparisonPages.Speed(book, null, UnitSettings.Imperial));
    }

    [Fact]
    public void TheFileNameNamesTheLoadsAndTheDate()
    {
        Assert.Equal("Compare loads Test load 1 vs Test load 2 2026-11-24", ComparisonPages.FileName(Page(2)));
        Assert.Equal("Compare loads 5 loads 2026-11-24", ComparisonPages.FileName(Page(5)));
    }

    /// <summary>
    /// Suggestion 62: the export draws the screen's two interval charts, a bar a load in its color with its figure a dot and the
    /// comparison's sentence beneath, and gives the shots to resolve a difference as a table; two loads still fit one page.
    /// </summary>
    [Fact]
    public void TheExportDrawsTheIntervalChartsAndTheShotsTable()
    {
        var page = Page(2);
        var scene = Assert.Single(ComparisonPages.Pages(page));
        var texts = Texts(scene);
        Assert.Equal(["Mean radius, the range each could be", "Sigma, the range each could be"], page.Charts.Select(c => c.Title));
        foreach (var chart in page.Charts)
        {
            Assert.Contains(chart.Title, texts);
            Assert.Contains(chart.LowWords, texts);
            Assert.Contains(chart.HighWords, texts);
            Assert.All(chart.Rows, r => Assert.InRange(r.Lower, chart.Low, chart.High));
            Assert.All(chart.Rows, r => Assert.InRange(r.Upper, chart.Low, chart.High));
        }

        Assert.Equal(page.Report.Groups.Select(g => g.MeanRadius), page.Charts[0].Rows);
        Assert.Equal(page.Report.Groups.Select(g => g.Rayleigh.Sigma), page.Charts[1].Rows);
        foreach (string heading in new[] { "Difference in spread", "Shots per load", "Rounds in all", "10 percent", "25 percent", "50 percent" })
        {
            Assert.Contains(heading, texts);
        }

        foreach (var row in page.Report.Resolve)
        {
            Assert.Contains(row.ShotsPerLoad.ToString(System.Globalization.CultureInfo.InvariantCulture), texts);
            Assert.Contains((2 * row.ShotsPerLoad).ToString(System.Globalization.CultureInfo.InvariantCulture), texts);
        }

        Assert.DoesNotContain(texts, t => t.Contains("shots per load;", StringComparison.Ordinal));
    }
}
