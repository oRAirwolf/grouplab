using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// Shots A, NOTES-FROM-PLANNING.md entry 280 section 2 (entry 278 features c and f): every shot with its offset across and up and down from
/// its aim point. A shot left out is left out of every figure (entry 278 section 5c), struck through here and dashed on the picture, and stays
/// on the record; the summary gives the counted figures and says the shooter left a shot out. "Share the table (CSV)" shares the shots as the
/// result's CSV does.
/// <para>
/// Entry 407 section 3 (concept 2B, Alan's pick): short rows, the shot, across, up or down and how far, and a tap opens the row as a card under
/// it, one at a time, with its bull (the nearest first and marked), its place in the firing order, Counted, where it came from, and "Delete,
/// not a hole", which keeps the mark, its provenance and Undo, as the desktop's does. A mark that is not a hole stays at the end of the list.
/// </para>
/// </summary>
internal sealed class ShotsPage : UserControl
{
    private readonly MarkingSession session;
    private readonly UnitSettings units;
    private readonly Action changed;
    private readonly Action back;
    private readonly GroupLab.Core.Gltd.Model.TargetDefinition? definition;
    private int? open;

    public ShotsPage(MarkingSession session, GroupLab.Core.Gltd.Model.TargetDefinition? definition, UnitSettings units, Action changed, Action back)
    {
        this.session = session;
        this.definition = definition;
        this.units = units;
        this.changed = changed;
        this.back = back;
        Show();
    }

    /// <summary>For the tests: the row open now, and opening one as a tap does.</summary>
    internal int? Open
    {
        get => open;
        set
        {
            open = value;
            Show();
        }
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

        var rows = ShotList.Rows(state);
        if (rows.Count == 0 || rows.All(r => r.AcrossInches is null && !r.NotAHole))
        {
            column.Children.Add(Screens.Line(ResultWords.NoOffsets));
        }
        else
        {
            column.Children.Add(Screens.Dim(ResultWords.ShotsIntro(state) + " Tap a shot to open it."));
            var table = new StackPanel();
            foreach (var row in rows)
            {
                table.Children.Add(Row(row));
                if (row.ShotId == open && state.Find(row.ShotId) is { } shot)
                {
                    table.Children.Add(Opened(state, shot));
                }
            }

            column.Children.Add(new Border { Child = table, Classes = { PhoneStyles.Card } }.Id("shots-table"));
        }

        var shared = Screens.Line("");
        column.Children.Add(Screens.Choice("Share the table (CSV)", () => shared.Text = SessionFiles.ShareCsv(session.State, definition) ?? ""));
        column.Children.Add(shared);
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }

    private Control Row(ShotListRow row)
    {
        var decoration = row.LeftOut ? TextDecorations.Strikethrough : null;
        string numbers = row.NotAHole ? "not a hole"
            : row.AcrossInches is { } a && row.UpInches is { } u && row.RadiusInches is { } r
                ? $"{units.Length(Math.Abs(a))} {(a >= 0 ? "right" : "left")}, {units.Length(Math.Abs(u))} {(u >= 0 ? "up" : "down")}, {units.Length(r)} out"
                : "";
        var words = new StackPanel { Spacing = 2 };
        words.Children.Add(new TextBlock { Text = row.Name + (row.LeftOut ? ", left out" : ""), FontWeight = row.ShotId == open ? FontWeight.Bold : FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        words.Children.Add(new TextBlock { Text = numbers, TextWrapping = TextWrapping.Wrap, TextDecorations = decoration, Classes = { PhoneStyles.Dim } });
        var button = new Button
        {
            Content = words,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            MinHeight = Screens.Touch,
            Margin = new Avalonia.Thickness(0, 2),
        };
        Avalonia.Automation.AutomationProperties.SetName(button, $"{row.Name}{(row.LeftOut ? ", left out" : "")}. {numbers}. Open it.");
        button.Click += (_, _) => Open = open == row.ShotId ? null : row.ShotId;
        return button;
    }

    /// <summary>Entry 407 section 3.2 and 3.5: the open row as a card under its shot.</summary>
    private Control Opened(MarkingState state, MarkedShot shot)
    {
        int id = shot.Id;
        var card = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(8, 4, 0, 10) };
        if (shot.NotAShot)
        {
            card.Children.Add(Screens.Dim("Marked as not a hole. The mark and where it came from are kept, and it counts in nothing."));
            card.Children.Add(Screens.Choice("It is a hole", () => Change(() => session.SetNotAShot(id, false))));
            return new Border { Child = card, Classes = { PhoneStyles.Card } };
        }

        var options = ShotList.Bulls(state, id);
        if (options.Count > 0)
        {
            var bull = new ComboBox { ItemsSource = options.Select(o => o.Words).ToList(), SelectedIndex = Math.Max(0, options.ToList().FindIndex(o => o.Index == shot.Bull)), HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = Screens.Touch };
            Avalonia.Automation.AutomationProperties.SetName(bull, "Bull");
            bull.SelectionChanged += (_, _) =>
            {
                var chosen = options[Math.Max(0, bull.SelectedIndex)];
                if (chosen.Index != session.State.Find(id)?.Bull)
                {
                    Change(() => session.AssignBull(id, chosen.Index));
                }
            };
            card.Children.Add(new StackPanel { Spacing = 4, Children = { Screens.Line("Bull"), bull } });
        }

        var fired = new ComboBox { ItemsSource = ShotList.FiredChoices(state), SelectedIndex = shot.Fired ?? 0, HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = Screens.Touch };
        Avalonia.Automation.AutomationProperties.SetName(fired, "Fired");
        fired.SelectionChanged += (_, _) =>
        {
            int? place = fired.SelectedIndex > 0 ? fired.SelectedIndex : null;
            if (place != session.State.Find(id)?.Fired)
            {
                Change(() => session.SetFired(id, place));
            }
        };
        card.Children.Add(new StackPanel { Spacing = 4, Children = { Screens.Line("Fired"), fired } });

        var counted = new CheckBox { Content = "Counted", IsChecked = shot.Exclusion is null, MinHeight = Screens.Touch };
        counted.IsCheckedChanged += (_, _) =>
        {
            bool on = counted.IsChecked == true;
            if (on != (session.State.Find(id)?.Exclusion is null))
            {
                Change(() => session.SetExclusion(id, on ? null : ExclusionReason.ByShooter));
            }
        };
        card.Children.Add(counted);

        // Entry 407 section 4.1: a reason already stored is still shown, read only.
        if (shot.Exclusion is { } reason && reason != ExclusionReason.ByShooter)
        {
            card.Children.Add(Screens.Dim("Left out as " + reason.InSentence() + "."));
        }

        card.Children.Add(Screens.Dim(ShotList.From(shot)));
        card.Children.Add(Screens.Choice("Delete, not a hole", () => Change(() => session.SetNotAShot(id, true))).Id("shots-delete"));
        return new Border { Child = card, Classes = { PhoneStyles.Card } };
    }

    /// <summary>A change from the open row: made, measured again by the result, and the list shown afresh after the list's own event.</summary>
    private void Change(Action change)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            change();
            changed();
            Show();
        });
    }
}
