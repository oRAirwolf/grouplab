using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.Android;

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

    public ComparePage(IReadOnlyList<SessionRecord> records, UnitSettings units, Action back)
    {
        // Entry 273: a tap on any number switches units everywhere; this page shows them again.
        void Follow() => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            this.units = App.Settings.LoadUnits();
            Draw();
        });
        AttachedToVisualTree += (_, _) => Shell.UnitsChanged += Follow;
        DetachedFromVisualTree += (_, _) => Shell.UnitsChanged -= Follow;

        this.units = units;
        var setup = CompareSessions.From(records, units);
        distance = setup.Distance;
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

            column.Children.Add(new ContentControl());
            column.Children.Add(new ContentControl());
            var list = new StackPanel { Spacing = 6 };
            foreach (var (group, detail) in report.Groups.Zip(setup.Groups, (g, s) => (g, s.Detail)))
            {
                list.Children.Add(new StackPanel { Spacing = 2, Children = { Screens.Line(group.Name), Screens.Dim(detail) } });
            }

            column.Children.Add(Screens.Card(list));
            var verdict = new StackPanel { Spacing = 6, Children = { Screens.Heading("What these shots can tell"), Screens.Line(report.Headline) } };
            foreach (string line in report.Explanation)
            {
                verdict.Children.Add(Screens.Dim(line));
            }

            column.Children.Add(new Border
            {
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(40, 232, 150, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
                BorderThickness = new Thickness(1),
                Child = verdict,
            });
            Draw();
        }

        column.Children.Add(Screens.Choice("Add or take out a load", back));
        Content = Screens.Page(column);
    }

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
        foreach (string name in new[] { "Mean radius", "Extreme spread", "CEP 90" })
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

        var rows = report.Groups.Select(g => figure switch
        {
            "Extreme spread" => new IntervalRow(g.Name, g.ExtremeSpread, null, null),
            "CEP 90" when g.Rayleigh.Cep(0.9) is var cep => new IntervalRow(g.Name, cep.Value, cep.Lower, cep.Upper),
            _ => new IntervalRow(g.Name, g.MeanRadius.Value, g.MeanRadius.Lower, g.MeanRadius.Upper),
        }).ToList();
        var chart = new IntervalChart { Rows = rows, Length = Size, Height = 60 + (44 * rows.Count) };
        int at = column.Children.IndexOf(column.Children.OfType<ContentControl>().First());
        column.Children[at] = new ContentControl { Content = chips };
        column.Children[at + 1] = Screens.Card(Screens.Heading(figure + ", with the range each could really be"), chart, Screens.Dim(chart.Description));
    }
}
