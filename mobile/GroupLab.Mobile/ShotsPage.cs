using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

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
        // Entry 280 section 2: the words are the desktop window's own (ResultWords).
        if (ResultWords.ShotsSummary(GroupAnalysis.Analyse(state), units) is { } summary)
        {
            column.Children.Add(Screens.Line(summary));
        }

        var rows = ShotOffsets.Table(state);
        if (rows.Count == 0)
        {
            column.Children.Add(Screens.Line(ResultWords.NoOffsets));
        }
        else
        {
            column.Children.Add(Screens.Dim(ResultWords.ShotsIntro(state)));
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
        string clicks = ResultWords.Clicks(row);
        var decoration = row.LeftOut ? TextDecorations.Strikethrough : null;
        var words = new StackPanel { Spacing = 2 };
        words.Children.Add(new TextBlock { Text = row.Label, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration });
        words.Children.Add(new TextBlock { Text = ResultWords.Offset(row, units), TextWrapping = TextWrapping.Wrap, TextDecorations = decoration, Classes = { PhoneStyles.Dim } });
        if (clicks.Length > 0)
        {
            words.Children.Add(new TextBlock { Text = clicks, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration, Classes = { PhoneStyles.Dim } });
        }

        var counted = new ToggleSwitch { IsChecked = !row.LeftOut, OnContent = "Counted", OffContent = "Left out", VerticalAlignment = VerticalAlignment.Center, MinHeight = Screens.Touch };
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
