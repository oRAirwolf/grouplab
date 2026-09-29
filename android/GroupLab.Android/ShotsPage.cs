using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// Shots A, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 features c and f): every shot with its offset across and up and down from
/// its aim point, the clicks that would bring it onto the aim from the scope's click value, and a "Counted" switch. A shot switched off is
/// left out of every figure (entry 278 section 5c), struck through here and dashed on the picture, and stays on the record; the summary gives
/// the counted figures and says the shooter left a shot out. "Share the table (CSV)" shares the shots as the result's CSV does.
/// </summary>
internal sealed class ShotsPage : UserControl
{
    private readonly MarkingSession session;
    private readonly UnitSettings units;
    private readonly Action changed;
    private readonly Action back;
    private readonly GroupLab.Core.Gltd.Model.TargetDefinition? definition;

    public ShotsPage(MarkingSession session, GroupLab.Core.Gltd.Model.TargetDefinition? definition, UnitSettings units, Action changed, Action back)
    {
        this.session = session;
        this.definition = definition;
        this.units = units;
        this.changed = changed;
        this.back = back;
        Show();
    }

    private void Show()
    {
        var state = session.State;
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Shots"));
        var report = GroupAnalysis.Analyse(state);
        if (report.Counted is { } counted)
        {
            string left = report.Excluded == 0 ? "" : string.Create(CultureInfo.CurrentCulture,
                $"; {report.Excluded} left out by the shooter, still on the record");
            string radius = counted.MeanRadius is { } mr ? ", mean radius " + units.Length(mr.Value) : "";
            column.Children.Add(Screens.Line(string.Create(CultureInfo.CurrentCulture, $"{counted.Shots} shots counted{radius}{left}.")));
        }

        var rows = ShotOffsets.Table(state);
        if (rows.Count == 0)
        {
            column.Children.Add(Screens.Line("These shots have no aim point to measure from, so there are no offsets to show."));
        }
        else
        {
            column.Children.Add(Screens.Dim(state.Rifle is null || state.ShotDistanceInches is null
                ? "Across and up from the aim point. With a rifle's click value and the distance, each shot also shows its clicks."
                : $"Across and up from the aim point, and the clicks that would bring each shot onto it, at {state.Rifle.DescribeClick()}."));
            var table = new StackPanel();
            foreach (var row in rows)
            {
                table.Children.Add(Row(row));
            }

            column.Children.Add(new Border { Child = table, Classes = { PhoneStyles.Card } });
        }

        var shared = Screens.Line("");
        column.Children.Add(Screens.Choice("Share the table (CSV)", () => shared.Text = SessionFiles.ShareCsv(session.State, definition) ?? ""));
        column.Children.Add(shared);
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }

    private Control Row(ShotOffsetRow row)
    {
        static string Way(double inches, string positive, string negative) => inches >= 0 ? positive : negative;
        string across = units.Length(Math.Abs(row.AcrossInches)) + " " + Way(row.AcrossInches, "right", "left");
        string up = units.Length(Math.Abs(row.UpInches)) + " " + Way(row.UpInches, "up", "down");
        string clicks = string.Join(", ", new[] { row.AcrossClicks?.Describe(), row.UpClicks?.Describe() }.Where(c => c is not null));
        var decoration = row.LeftOut ? TextDecorations.Strikethrough : null;
        var words = new StackPanel { Spacing = 2 };
        words.Children.Add(new TextBlock { Text = "Shot " + row.Label, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration });
        words.Children.Add(new TextBlock { Text = across + ", " + up, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration, Classes = { PhoneStyles.Dim } });
        if (clicks.Length > 0)
        {
            words.Children.Add(new TextBlock { Text = clicks, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration, Classes = { PhoneStyles.Dim } });
        }

        var counted = new ToggleSwitch { IsChecked = !row.LeftOut, OnContent = "Counted", OffContent = "Left out", VerticalAlignment = VerticalAlignment.Center };
        counted.IsCheckedChanged += (_, _) =>
        {
            session.SetExclusion(row.ShotId, counted.IsChecked == true ? null : ExclusionReason.ByShooter);
            changed();
            Show();
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10, Margin = new Avalonia.Thickness(0, 6) };
        Grid.SetColumn(counted, 1);
        grid.Children.Add(words);
        grid.Children.Add(counted);
        return grid;
    }
}
