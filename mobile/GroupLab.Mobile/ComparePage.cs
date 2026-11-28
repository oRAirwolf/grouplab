using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 4, "one figure at a time, A" (Alan's choice), reached from Sessions: a figure picker as chips
/// (Mean radius, Extreme spread, CEP 90), a card drawing each load with the range it could really be on one shared scale (the desktop's
/// own chart), the load list, then an amber card headed "What these shots can tell" with the desktop's plain verdict, and "Add or take out
/// a load". The sessions are made into groups by the shared code (<see cref="CompareSessions"/>) and compared by <see cref="LoadComparison"/>,
/// as on the desktop.
/// </summary>
internal sealed class ComparePage : UserControl
{
    private readonly StackPanel column = new() { Spacing = 12 };
    private readonly LoadComparisonReport? report;
    private UnitSettings units;
    private readonly double? distance;
    private string figure = "Mean radius";

    /// <summary>Suggestion 61: the page the comparison exports, made when asked; null where there is no comparison.</summary>
    private readonly Func<GroupLab.Core.Reporting.ComparisonPage>? exportPage;

    /// <summary>What the last export said.</summary>
    private readonly TextBlock exportSaid = Screens.Line("");

    /// <summary>Where the figure chips go in the column, with the chart's card after them.</summary>
    private int chipsAt;

    /// <summary>The widest the words run: the whole width, as on every other page since entry 376 item B2 (entry 312 section 1 held it to 640).</summary>
    internal const double Reading = double.PositiveInfinity;

    public ComparePage(IReadOnlyList<SessionRecord> records, UnitSettings units, Action back, Action? sessions = null)
    {
        // Entry 273: a tap on any number switches units everywhere; this page shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = Phone.Settings.LoadUnits();
            Draw();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.units = units;
        var setup = CompareSessions.From(records, units);
        distance = setup.Distance;
        // Entry 312 section 1: a way back to Sessions at the top, which the iPad showed none of; Android's own Back goes to Capture.
        column.Children.Add(Screens.BackButton("Back to Sessions", sessions ?? back));
        column.Children.Add(Screens.Title("Compare loads"));
        string? refusal = setup.Refusal;
        if (refusal is null)
        {
            try
            {
                report = LoadComparison.Compare([.. setup.Groups.Select(g => (g.Name, g.Offsets))], units.Length);
            }
            catch (ArgumentException e)
            {
                refusal = e.Message.Split(" (Parameter", StringSplitOptions.None)[0];
            }
        }

        DiagnosticLog.Info("compare.sessions", ("groups", setup.Groups.Count), ("phone", true));
        if (refusal is not null || report is null)
        {
            column.Children.Add(Screens.Line(refusal ?? "These sessions cannot be compared."));
        }
        else
        {
            if (setup.Footing is { } footing)
            {
                column.Children.Add(Screens.Dim(footing));
            }

            // Entry 309 section 2: each load's group about its own center at one scale, in its color; a tap stacks them on one center.
            // Entry 465 item 7B: with two loads, what the difference means under the pictures, or beside them on a wide screen.
            Groups = new LoadGroups([.. report.Groups.Select(g => new LoadGroup(g.Name, g.Offsets, g.Centre, g.MeanRadius.Value))], Size, cell: 150, touch: Screens.Touch, fill: true, report: report);
            column.Children.Add(Screens.Card(Groups));
            chipsAt = column.Children.Count;
            column.Children.Add(new ContentControl());
            column.Children.Add(new ContentControl());
            var list = new StackPanel { Spacing = 6 };
            foreach (var (group, detail) in report.Groups.Zip(setup.Groups, (g, s) => (g, s.Detail)))
            {
                list.Children.Add(new StackPanel { Spacing = 2, Children = { Screens.Line(group.Name), Screens.Quiet(detail) } });
            }

            column.Children.Add(Screens.Card(list));

            // Entry 456 section 3 and request 103: Top Gun for each compared load, and each group beside it on its basis.
            var book = PhoneAnalysis.Store().LoadBook();
            var topGun = GroupLab.Core.Ballistics.TopGun.Lines(records.Zip(setup.Groups, (r, g) => (r.Rifle, r.Load, g.Name, g.Offsets)), book, setup.Distance);
            if (topGun.Count > 0)
            {
                var card = new StackPanel { Spacing = 6, Children = { Screens.Heading("Top Gun") } };
                foreach (string line in topGun)
                {
                    card.Children.Add(Screens.Line(line));
                }

                card.Children.Add(Screens.Dim(GroupLab.Core.Ballistics.TopGun.Note));
                column.Children.Add(Screens.Card(card));
            }

            var verdict = new StackPanel { Spacing = 6, Children = { Screens.Heading("What these shots can tell"), LoadGroups.Key([.. report.Groups.Select(g => g.Name)]), Screens.Line(report.Headline) } };
            // Entry 312 section 3: plain words first; the ratio with its interval and the power sentence behind Details.
            foreach (string line in report.Plain)
            {
                verdict.Children.Add(Screens.Dim(line));
            }

            var exact = new StackPanel { Spacing = 6 };
            foreach (string line in report.Details)
            {
                exact.Children.Add(Screens.Dim(line));
            }

            verdict.Children.Add(new Expander
            {
                Header = new TextBlock { Text = "Details", Classes = { PhoneStyles.Dim } },
                Content = exact,
                IsExpanded = false,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                MinHeight = Screens.Touch,
            });

            column.Children.Add(new Border
            {
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(40, 232, 150, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
                BorderThickness = new Thickness(1),
                Child = verdict,
            });

            // Suggestion 63: where each center sits from the aim, drawn with its ranges, and each figure as a short block beneath.
            Centres = CentreChart.Part(report, Size, compact: true, side: 240);
            column.Children.Add(Screens.Card(Centres));

            // Suggestion 61: the comparison as one page, the computer's own, shared as a PDF or as a picture of the same page to post.
            var loads = setup.Groups.Select(g => new ExportedLoad(g.Name, g.Detail, ComparisonPages.Speed(book, g.Load, units))).ToList();
            string summary = $"{report.Groups.Count} loads, {report.Groups.Sum(g => g.Shots)} shots, in the order chosen." + (setup.Footing is { } said ? " " + said : "");
            var shown = report;
            exportPage = () => ComparisonPages.For(shown, loads, summary, topGun, this.units, distance, OnePageReports.PaperFor(AppSettingsStore.LetterPaper()),
                DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), AppInfo.ShortVersion);
            column.Children.Add(Screens.Card(
                Screens.Heading("Export"),
                Screens.Dim("One page: each load's group, its figures with their ranges, what the difference means and the tests, with GroupLab and grouplab.org at the foot."),
                Screens.Primary("Share as a picture", () => Export(picture: true, gallery: false)).Id("compare-share-picture"),
                Screens.Choice("Share as a PDF", () => Export(picture: false, gallery: false)).Id("compare-share-pdf"),
                Screens.Choice("Save the picture to the gallery", () => Export(picture: true, gallery: true)).Id("compare-save-picture"),
                exportSaid));
            Draw();
        }

        column.Children.Add(Screens.Choice("Add or take out a load", back));
        Content = Screens.Page(column);

        // Entry 312 section 1: the column spans the screen, and everything but the plots and the chart keeps to the reading width.
        column.MaxWidth = double.PositiveInfinity;
        foreach (var child in column.Children.Where(c => !Wide(c)))
        {
            child.MaxWidth = Reading;
        }
    }

    /// <summary>
    /// Suggestion 61: the comparison as a PDF through the share sheet, or as a picture of the same page through the share sheet or into the
    /// gallery. The file goes in the shared folder, where only the newest shared file is kept.
    /// </summary>
    internal void Export(bool picture, bool gallery)
    {
        if (exportPage is null)
        {
            return;
        }

        try
        {
            var page = exportPage();
            byte[] bytes = picture ? ComparisonPages.Picture(page) : ComparisonPages.Write(page);
            DiagnosticLog.Info("compare.export", ("picture", picture), ("gallery", gallery), ("bytes", bytes.Length));
            if (!picture)
            {
                exportSaid.Text = Phone.Platform.SharePdf(bytes, ComparisonPages.FileName(page)) ?? "";
                return;
            }

            string shared = Path.Combine(Phone.Platform.CacheFolder, "shared");
            if (Directory.Exists(shared))
            {
                Directory.Delete(shared, recursive: true);
            }

            Directory.CreateDirectory(shared);
            string file = Path.Combine(shared, ComparisonPages.FileName(page) + ".png");
            File.WriteAllBytes(file, bytes);
            exportSaid.Text = gallery
                ? Phone.Platform.SaveToGallery(file, "image/png") ?? "Saved to the gallery, in Pictures, GroupLab."
                : Phone.Platform.ShareFile(file, "image/png", "Share the comparison") ?? "";
        }
        catch (IOException ex)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "compare.export", ex);
            ProblemSheet.Stop(exportSaid, exportSaid, "The comparison could not be exported", "The comparison could not be exported: " + ex.Message);
        }
    }

    /// <summary>Whether a part of the page takes the whole width: the card of each load's group and the chart's card.</summary>
    private static bool Wide(Control part) => part is Border { Child: StackPanel inside } && inside.Children.Any(c => c is LoadGroups or IntervalChart);

    private string Size(double inches) => distance is { } d && units.Angle(inches, d) is { } angle
        ? angle.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture) + " " + UnitSettings.Symbol(units.Angular)
        : units.Length(inches);

    private void Draw()
    {
        if (report is null)
        {
            return;
        }

        var chips = new WrapPanel();
        foreach (string name in Figures)
        {
            var chip = new Button { Content = name, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
            if (name == figure)
            {
                chip.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
            }

            chip.Click += (_, _) =>
            {
                figure = name;
                Draw();
            };
            chips.Children.Add(chip);
        }

        var chart = Chart(report, figure, Size);
        chart.RowsAreLoads = true;
        int at = chipsAt;
        column.Children[at] = new ContentControl { Content = chips, MaxWidth = Reading };
        // Extreme spread has no range here, so its heading does not promise one (entry 295 section 1.4).
        string heading = chart.HasRanges ? figure + ", with the range each could really be" : figure + ", as measured";
        column.Children[at + 1] = Screens.Card(Screens.Heading(heading), chart, Screens.Dim(chart.Description));
    }

    /// <summary>
    /// The chart of one figure, entry 295 section 1: each load's name on its own line with its range and value beneath, at the phone's
    /// secondary text size, as tall as its rows and no taller, and saying in words only what the verdict card agrees with.
    /// </summary>
    internal static IntervalChart Chart(LoadComparisonReport report, string figure, Func<double, string> size, double textSize = GroupLab.App.Theme.Tokens.HeadingSize)
    {
        var rows = report.Groups.Select(g => figure switch
        {
            "Extreme spread" => new IntervalRow(g.Name, g.ExtremeSpread, null, null),
            "CEP 90" when g.Rayleigh.Cep(0.9) is var cep => new IntervalRow(g.Name, cep.Value, cep.Lower, cep.Upper),
            _ => new IntervalRow(g.Name, g.MeanRadius.Value, g.MeanRadius.Lower, g.MeanRadius.Upper),
        }).ToList();
        var chart = new IntervalChart { Rows = rows, Length = size, TextSize = GroupLab.App.Theme.TextSize.Of(textSize), Margin = new Thickness(0, 4) };
        chart.Says = LoadComparison.ChartSays(report, figure, chart.HasRanges);
        return chart;
    }

    /// <summary>The card of each load's group, for the tests.</summary>
    internal LoadGroups? Groups { get; }

    /// <summary>Suggestion 63: the chart of the centers and its blocks, for the tests.</summary>
    internal StackPanel? Centres { get; }

    /// <summary>The figures the chips offer, in their order.</summary>
    internal static readonly string[] Figures = ["Mean radius", "Extreme spread", "CEP 90"];
}
