using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Android;

/// <summary>
/// "Fudd buster mode", NOTES-FROM-PLANNING.md entry 279 section 3 (Unholy's idea and name, kept by Alan in entry 280 section 3) as page A of
/// entry 281 section 2: one page, three sections, from the result's own shots. The words are <see cref="FuddBusterWords"/>'s, the desktop's
/// window says the same, and the examples are the same every time this result is opened.
/// </summary>
internal sealed class FuddBusterPage : UserControl
{
    public FuddBusterPage(MarkingState state, UnitSettings units, Action back)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(FuddBusterWords.Title));
        column.Children.Add(Screens.Dim(FuddBusterWords.Credit));
        var shots = Shots(state);
        if (FuddBuster.Of(shots, state.Rifle, state.ShotDistanceInches) is not { } lesson)
        {
            column.Children.Add(Screens.Line(string.Create(CultureInfo.CurrentCulture, $"It needs at least {FuddBuster.LeastShots} shots; this result has {shots.Count}.")));
        }
        else
        {
            var three = FuddBusterWords.Threes(lesson, units);
            var plots = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8 };
            var tight = Plot(shots, lesson.Tightest, units);
            var wide = Plot(shots, lesson.Widest, units);
            Grid.SetColumn(wide, 1);
            plots.Children.Add(tight);
            plots.Children.Add(wide);
            column.Children.Add(Section("Three shots from the same rifle", [.. three.Select(Screens.Line), plots]));
            column.Children.Add(Section("Averaging small groups", [.. FuddBusterWords.Averages(lesson, units).Select(Screens.Line)]));
            column.Children.Add(Section("Chasing the zero", [.. FuddBusterWords.Chase(lesson, units).Select(Screens.Line)]));
        }

        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }

    /// <summary>The counted shots in the order they were marked, as offsets from their aim points in inches.</summary>
    internal static List<PointD> Shots(MarkingState state)
    {
        var counted = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s)).ToList();
        return [.. GroupAnalysis.CompositeOffsets(state, counted)];
    }

    private static Border Section(string heading, IEnumerable<Control> lines)
    {
        var inside = new StackPanel { Spacing = 8, Children = { Screens.Heading(heading) } };
        foreach (var line in lines)
        {
            inside.Children.Add(line);
        }

        return new Border { Child = inside, Classes = { PhoneStyles.Card } };
    }

    /// <summary>Every shot, the three drawn solid and the rest hollow and dim, as the plot draws a shot left out.</summary>
    private static CompositePlot Plot(IReadOnlyList<PointD> shots, ThreeShots three, UnitSettings units) => new()
    {
        Height = 200,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Shots = [.. shots.Select((o, k) => new PlotShot(k, (k + 1).ToString(CultureInfo.InvariantCulture), null, o, !three.Shots.Contains(k)))],
        Centre = GroupStatistics.Centre([.. three.Shots.Select(i => shots[i])]),
        Length = inches => units.Length(inches),
        ShowKey = false,
    };
}
