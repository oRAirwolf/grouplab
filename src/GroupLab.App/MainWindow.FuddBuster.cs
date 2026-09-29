using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// "Fudd buster mode", NOTES-FROM-PLANNING.md entry 279 section 3: Unholy's idea and name, kept by Alan (entry 280 section 3), as page A of
/// entry 281 section 2, which on the desktop is a window of its own. The same three sections and the same words as the phone's page
/// (<see cref="FuddBusterWords"/>), from the analysis's counted shots, offered from twenty shots in the full figures beside Shots Needed to
/// Zero.
/// </summary>
public sealed partial class MainWindow
{
    private void ShowFuddBusterButton(MarkingState state)
    {
        var counted = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s)).ToList();
        if (counted.Count < FuddBuster.LeastShots)
        {
            return;
        }

        fullFigures.Children.Add(FieldLabel(FuddBusterWords.Title));
        fullFigures.Children.Add(Row(Button("Why a few shots mislead, shown with these shots", () => _ = OpenFuddBuster(state))));
    }

    /// <summary>The window, for the headless tests: its text in order.</summary>
    internal Window? FuddBusterWindow { get; private set; }

    private async Task OpenFuddBuster(MarkingState state)
    {
        var counted = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s)).ToList();
        var shots = GroupAnalysis.CompositeOffsets(state, counted);
        if (FuddBuster.Of(shots, state.Rifle, state.ShotDistanceInches) is not { } lesson)
        {
            return;
        }

        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        body.Children.Add(new TextBlock { Text = FuddBusterWords.Title, Classes = { AppStyles.Section } });
        body.Children.Add(Note(FuddBusterWords.Credit));
        void Section(string heading, IEnumerable<string> lines, Control? extra = null)
        {
            body.Children.Add(FieldLabel(heading));
            foreach (string line in lines)
            {
                body.Children.Add(new TextBlock { Text = line, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            }

            if (extra is not null)
            {
                body.Children.Add(extra);
            }
        }

        var plots = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = Tokens.Space8 };
        var wide = FuddPlot(shots, lesson.Widest);
        Grid.SetColumn(wide, 1);
        plots.Children.Add(FuddPlot(shots, lesson.Tightest));
        plots.Children.Add(wide);
        Section("Three shots from the same rifle", FuddBusterWords.Threes(lesson, units), plots);
        Section("Averaging small groups", FuddBusterWords.Averages(lesson, units));
        Section("Chasing the zero", FuddBusterWords.Chase(lesson, units));

        FuddBusterWindow = new Window
        {
            Title = FuddBusterWords.Title,
            Width = 640,
            Height = 760,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ScrollViewer { Content = body },
        };
        Diagnostics.DiagnosticLog.Info("dialog.open", ("dialog", "fudd-buster"), ("shots", lesson.Shots));
        await FuddBusterWindow.ShowDialog(this);
    }

    private CompositePlot FuddPlot(IReadOnlyList<PointD> shots, ThreeShots three) => new()
    {
        Height = 240,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Shots = [.. shots.Select((o, k) => new PlotShot(k, (k + 1).ToString(CultureInfo.InvariantCulture), null, o, !three.Shots.Contains(k)))],
        Centre = GroupStatistics.Centre([.. three.Shots.Select(i => shots[i])]),
        Length = inches => units.Length(inches),
        ShowKey = false,
    };
}
