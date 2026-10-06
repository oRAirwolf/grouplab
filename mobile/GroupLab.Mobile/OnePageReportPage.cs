using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;

namespace GroupLab.Mobile;

/// <summary>
/// The report on the phone, NOTES-FROM-PLANNING.md entry 280 section 2 (board Report; entry 278 feature e): one dated page, Letter or A4 as
/// the phone's region prints, with the picture, the plot centered on the group with its grid stated, the figures table, the load and
/// equipment line and the confidence sentence, shared or printed. The page is <see cref="OnePageReports"/>', the desktop's own.
/// </summary>
internal sealed class OnePageReportPage : UserControl
{
    private readonly MarkingState state;
    private readonly string title;
    private readonly string date;
    private readonly UnitSettings units;
    private readonly TextBlock said = Screens.Line("");
    private (byte[] Pdf, OnePageReport Report)? made;

    public OnePageReportPage(MarkingState state, string title, string date, UnitSettings units, Action back)
    {
        this.state = state;
        this.title = title;
        this.date = date;
        this.units = units;
        var paper = Paper;
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Report"));
        column.Children.Add(Screens.Line($"One page on {paper}, dated {date}: the picture, a plot centered on the group with its grid, the figures, the load and equipment, and how sure the figures are."));
        column.Children.Add(Screens.Primary("Share", () => said.Text = Make() is { } m ? Phone.Platform.SharePdf(m.Pdf, OnePageReports.FileName(m.Report)) ?? "" : said.Text).Id("report-share"));
        column.Children.Add(Screens.Choice("Save on the phone", () => said.Text = Make() is { } m ? Phone.Platform.SavePdf(m.Pdf, OnePageReports.FileName(m.Report)) : said.Text).Id("report-save"));
        column.Children.Add(Screens.Choice("Print", () => said.Text = Make() is { } m ? Phone.Platform.PrintPdf(m.Pdf, OnePageReports.FileName(m.Report), paper) ?? "" : said.Text));
        column.Children.Add(said);

        // Entry 376 item B6, Alan: the report shown inside GroupLab, the PDF's own pages, with Share and Save above it; nothing to export first.
        if (Make() is { } shown)
        {
            var pages = Phone.Platform.PdfPages(shown.Pdf, PageWidth);
            foreach (var png in pages)
            {
                using var stream = new MemoryStream(png);
                column.Children.Add(new Border
                {
                    Child = new Avalonia.Controls.Image { Source = new Avalonia.Media.Imaging.Bitmap(stream), Stretch = Avalonia.Media.Stretch.Uniform },
                    Classes = { PhoneStyles.Card },
                }.Id("report-page"));
            }

            if (pages.Count == 0)
            {
                column.Children.Add(Screens.Dim("This phone cannot show the PDF here; Share opens it in another app."));
            }
        }

        column.Children.Add(Screens.Dim("The full report, every figure and every why on two pages or more, is on the computer."));
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }

    /// <summary>How wide each page is drawn, in pixels: sharp on a tablet, and scaled down to the screen.</summary>
    private const int PageWidth = 1600;

    /// <summary>Letter where the phone's region prints on Letter, A4 everywhere else, as the Targets screen starts.</summary>
    private static GroupLab.Core.Gltd.Model.PageSize Paper => OnePageReports.PaperFor(AppSettingsStore.LetterRegion(AppSettingsStore.Region()));

    /// <summary>The PDF, made once; null with the reason said where it could not be.</summary>
    private (byte[] Pdf, OnePageReport Report)? Make()
    {
        if (made is not null)
        {
            return made;
        }

        try
        {
            var picture = state.ImagePath is { } path ? ReportPicture.From(path, state.ViewQuarterTurns) : null;
            var report = OnePageReports.For(state, title, date, units, Paper, picture, "GroupLab " + AppInfo.ShortVersion);
            made = (OnePageReports.Write(report), report);
            DiagnosticLog.Info("report.one-page", ("bytes", made.Value.Pdf.Length), ("picture", picture is not null));
            return made;
        }
        catch (IOException ex)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "report.one-page", ex);
            ProblemSheet.Stop(said, said, "The report could not be made", "The report could not be made: " + ex.Message);
            return null;
        }
    }
}
