using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Reporting;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 280 section 2 (board Report, entry 278 feature e): the one-page report. One dated page, Letter or A4, with the
/// picture, the plot centred on the group with its grid stated, the figures table, the load and equipment line and the confidence sentence;
/// the figures are the analysis's counted ones.
/// </summary>
public class OnePageReportTests
{
    private static readonly PointD[] Group = [new(0.3, 0.1), new(0.1, -0.2), new(0.5, 0.2), new(0.2, 0.3), new(0.4, -0.1), new(0.25, 0.05)];

    private static MarkingState State() =>
        ShotCsv.Marking(Group, 3600) with { Rifle = new Rifle("Test rifle", 0.25, AngularUnit.Moa), Load = "Test load", Calibre = new Calibre(".308 Winchester", 0.308) };

    private static IEnumerable<string> Texts(Scene page) => page.Items.OfType<TextRun>().Select(t => t.Text);

    [Fact]
    public void ItIsOneDatedPageWithEverySectionTheBoardDraws()
    {
        var picture = new DocumentImage([0xFF, 0xD8, 0xFF, 0xD9], 400, 300);
        var report = OnePageReports.For(State(), "Test sheet", "2026-09-29", UnitSettings.Imperial, PageSize.Letter, picture, "GroupLab 1.0.0");
        var page = OnePageReports.Page(report);
        var texts = Texts(page).ToList();
        Assert.Equal(2159 * 2, page.Width);
        Assert.Equal(2794 * 2, page.Height);
        Assert.Contains("Test sheet", texts);
        Assert.Contains("GroupLab one-page report, dated 2026-09-29", texts);
        foreach (string heading in new[] { "Figures", "Load and equipment", "How sure" })
        {
            Assert.Contains(heading, texts);
        }

        Assert.Contains("Mean radius", texts);
        Assert.Contains("Extreme spread, center to center", texts);
        Assert.Contains("Center from aim", texts);
        Assert.Contains(texts, t => t.StartsWith("Rifle: Test rifle, 0.25 MOA a click; Load: Test load; Caliber: .308 Winchester; Distance: 100 yd", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.StartsWith("From 6 shots, the true mean radius", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.StartsWith("Grid squares ", StringComparison.Ordinal));
        Assert.Single(page.Items.OfType<ImageBox>());
        Assert.All(page.Items.OfType<TextRun>(), t => Assert.InRange(t.Baseline, 0, page.Height));
        Assert.All(page.Items.OfType<TextRun>(), t => Assert.True(ReportWriter.Printable(t.Text), t.Text));
    }

    [Fact]
    public void TheFiguresAreTheAnalysisCountedOnesAndALeftOutShotIsInNone()
    {
        var session = new MarkingSession();
        session.Load(ShotCsv.Marking([.. Group, new PointD(3, 0)], 3600));
        session.SetExclusion(session.State.Shots[^1].Id, ExclusionReason.ByShooter);
        var report = OnePageReports.For(session.State, "Test", "2026-09-29", UnitSettings.Imperial, PageSize.Letter, null, "");
        var counted = GroupAnalysis.Analyse(session.State).Counted!;
        Assert.Equal(UnitSettings.Imperial.Length(counted.MeanRadius!.Value), report.Figures.Single(f => f.Figure == "Mean radius").Size);
        Assert.Equal("6 counted, 1 left out by the shooter", report.Figures.Single(f => f.Figure == "Shots").Size);
        Assert.Equal(7, report.Plot.Shots.Count);
        Assert.Single(report.Plot.Shots, s => s.Excluded);
    }

    [Fact]
    public void ThePlotIsCentredOnTheGroupAndItsGridIsWholeSteps()
    {
        var plot = OnePageReports.Plot(State(), UnitSettings.Imperial);
        var centre = GroupStatistics.Centre(Group);
        Assert.Equal(centre.X, plot.Centre.X, 9);
        Assert.Equal(centre.Y, plot.Centre.Y, 9);
        Assert.True(plot.Aim);
        double squares = 2 * plot.HalfWidthInches / plot.GridInches;
        Assert.Equal(Math.Round(squares), squares, 9);
        Assert.InRange(squares, 2, 10);
        Assert.All(plot.Shots, s => Assert.InRange(Math.Abs(s.OffsetInches.X - centre.X), 0, plot.HalfWidthInches));
        Assert.StartsWith("Grid squares 0.", plot.GridWords, StringComparison.Ordinal);
        Assert.Contains(" in; the plot is ", plot.GridWords, StringComparison.Ordinal);

        var metric = OnePageReports.Plot(State(), UnitSettings.Metric);
        Assert.Contains(" cm", metric.GridWords, StringComparison.Ordinal);
    }

    [Fact]
    public void A4IsUsedWhereThePaperIsA4AndTheWholePdfIsOnePage()
    {
        Assert.Equal(PageSize.A4, OnePageReports.PaperFor(letterRegion: false));
        Assert.Equal(PageSize.Letter, OnePageReports.PaperFor(letterRegion: true));
        var report = OnePageReports.For(State(), "Test", "2026-09-29", UnitSettings.Metric, PageSize.A4, null, "GroupLab 1.0.0");
        var page = OnePageReports.Page(report);
        Assert.Equal(2100 * 2, page.Width);
        Assert.Equal(2970 * 2, page.Height);
        Assert.Contains("No picture: the shots were entered without one.", Texts(page));
        byte[] pdf = OnePageReports.Write(report);
        Assert.Equal(1, PDFtoImage.Conversion.GetPageCount(pdf));
        Assert.DoesNotContain("?", PdfWriter.Content(page), StringComparison.Ordinal);
    }

    [Fact]
    public void BelowTheShotsASpreadNeedsTheConfidenceSentenceSaysSo()
    {
        var report = OnePageReports.For(ShotCsv.Marking([new PointD(0, 0), new PointD(0.2, 0.1)], 3600), "Test", "2026-09-29", UnitSettings.Imperial, PageSize.Letter, null, "");
        Assert.Equal(GroupAnalysis.Analyse(ShotCsv.Marking([new PointD(0, 0), new PointD(0.2, 0.1)], 3600)).Counted!.DispersionWithheld, report.Confidence);
        Assert.DoesNotContain(report.Figures, f => f.Figure == "Mean radius");
        Assert.Equal("No rifle, load, caliber or distance was recorded with this target.", OnePageReports.Equipment(MarkingState.Empty, UnitSettings.Imperial));
    }
}
