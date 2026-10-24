using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 2, "tap the bulls on the sheet, A" (Alan's choice): the sheet's own layout, row by row, every bull a
/// numbered tap target of at least 44 px. Entry 407 section 2 (concept 1A, Alan's pick) makes it the phone's half of the Groups section, the
/// same two parts as the desktop's, cards full width:
/// <list type="number">
/// <item>"How did you shoot this sheet?", three answers. Under them the bulls left blank, found by GroupLab, and tapping a bull on the sheet
/// corrects them; with "Some bulls took two or more" a tap marks a bull that took two instead.</item>
/// <item>"Loads on this sheet", four buttons: one load, one a row, one a column, or bulls picked on the sheet with a load set on them.</item>
/// </list>
/// A line under both reads back what GroupLab will make of it. The rules are the desktop's (<see cref="SheetReading"/>), so each shot is
/// measured from its own bull the same way on both. Every change is made at once and measured again when the page closes.
/// </summary>
internal sealed class BullsPage : UserControl
{
    /// <summary>What a tap on a bull of the sheet does.</summary>
    internal enum Taps
    {
        FiredAt,
        TookTwo,
        ForALoad,
    }

    private readonly MarkingSession session;
    private readonly IReadOnlyList<BullAim> bulls;
    private readonly IReadOnlyList<IReadOnlyList<int>> rows;
    private readonly HashSet<int> picked = [];
    private readonly StackPanel sheet = new() { Spacing = 8 };
    private readonly StackPanel patterns = new() { Spacing = 8 };
    private readonly StackPanel loads = new() { Spacing = 8 };
    private readonly TextBlock unused = Screens.Line("").Id("bulls-unused");
    private readonly TextBlock mode = Screens.Dim("");
    private readonly TextBlock readBack = Screens.Line("").Id("bulls-read-back");
    private readonly IReadOnlyList<string> loadNames;
    private Taps taps = Taps.FiredAt;
    private LoadLayout layout;

    public BullsPage(MarkingSession session, Action changed, Action back)
    {
        this.session = session;
        bulls = [.. session.State.Bulls.Where(b => b.Scoring)];
        rows = AimedBulls.Rows(bulls);
        layout = SheetReading.LayoutOf(session.State);
        taps = SheetReading.PatternOf(session.State.Rule) == ShotPattern.SomeTookMore ? Taps.TookTwo : Taps.FiredAt;
        loadNames = ["No load", .. LoadNames()];
        var opened = session.State;

        var column = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Title("How did you shoot this sheet?"),
                patterns,
                Screens.Card(sheet),
                mode,
                unused,
                Screens.Heading("Loads on this sheet"),
                loads,
                readBack,
                Screens.Primary("Done", () =>
                {
                    // Entry 472 section 1: Android's back presses Done, so a visit that changed nothing saves nothing.
                    if (!ReferenceEquals(opened, session.State))
                    {
                        changed();
                    }

                    back();
                }).Id("bulls-done").LeavesPage(),
            },
        };
        Content = Screens.Page(column);
        Draw();
    }

    /// <summary>The loads in the record book, the same list the desktop offers.</summary>
    private static IEnumerable<string> LoadNames()
    {
        try
        {
            return PhoneAnalysis.Store().LoadBook().Loads.Select(l => l.Name);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or Microsoft.Data.Sqlite.SqliteException)
        {
            return [];
        }
    }

    /// <summary>For the tests: the marking this page changes.</summary>
    internal MarkingSession Session => session;

    /// <summary>For the tests: the answer, the line under the sheet and the read-back.</summary>
    internal ShotPattern Pattern => SheetReading.PatternOf(session.State.Rule);

    internal string UnusedText => unused.Text ?? "";

    internal string ReadBackText => readBack.Text ?? "";

    /// <summary>For the tests: answers "How did you shoot this sheet?" as a tap on its card does.</summary>
    internal void Choose(ShotPattern pattern)
    {
        session.SetAssignmentRule(SheetReading.Choose(session.State, pattern));
        taps = pattern == ShotPattern.SomeTookMore ? Taps.TookTwo : Taps.FiredAt;
        Draw();
    }

    /// <summary>For the tests: a tap on a bull of the sheet.</summary>
    internal void Tap(int bull)
    {
        switch (taps)
        {
            case Taps.TookTwo:
                session.SetAssignmentRule(SheetReading.TapDoubled(session.State, bull));
                break;
            case Taps.ForALoad:
                if (!picked.Remove(bull))
                {
                    picked.Add(bull);
                }

                break;
            default:
                session.SetAssignmentRule(SheetReading.TapFiredAt(session.State, bull));
                break;
        }

        Draw();
    }

    /// <summary>For the tests: chooses how the loads are laid out.</summary>
    internal void Lay(LoadLayout chosen)
    {
        layout = chosen;
        taps = chosen == LoadLayout.PickBulls ? Taps.ForALoad : SheetReading.PatternOf(session.State.Rule) == ShotPattern.SomeTookMore ? Taps.TookTwo : Taps.FiredAt;
        Draw();
    }

    /// <summary>For the tests: puts a load on a group of bulls, as its list does.</summary>
    internal void SetLoad(IReadOnlyList<int> group, string? load)
    {
        SheetReading.SetLoad(session, group, load);
        Draw();
    }

    private void Draw()
    {
        var state = session.State;
        var pattern = SheetReading.PatternOf(state.Rule);
        patterns.Children.Clear();
        foreach (var (p, words, line) in SheetReading.Patterns)
        {
            var radio = Screens.Radio("shot-pattern", words, p == pattern);
            radio.HorizontalAlignment = HorizontalAlignment.Stretch;
            var chosen = p;
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && chosen != SheetReading.PatternOf(session.State.Rule))
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => Choose(chosen));
                }
            };
            patterns.Children.Add(Screens.Card(radio, Screens.Dim(line)));
        }

        var fired = SheetReading.FiredAt(state.Rule, state.Bulls);
        var unusedNow = SheetReading.Unused(state).ToHashSet();
        var doubled = SheetReading.Doubled(state.Rule);
        sheet.Children.Clear();
        foreach (var row in rows)
        {
            var line = new UniformGrid { Rows = 1, Columns = rows.Max(r => r.Count), HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (int index in row)
            {
                var bull = bulls.First(b => b.Index == index);
                bool on = taps switch
                {
                    Taps.TookTwo => doubled.Contains(index),
                    Taps.ForALoad => picked.Contains(index),
                    _ => fired?.Contains(index) ?? !unusedNow.Contains(index),
                };
                string? load = state.Subgroups?.For(index);
                var button = new Button
                {
                    Content = doubled.Contains(index) ? bull.Label + " ×2" : bull.Label,
                    MinWidth = 44,

                    // Entry 461 section 2.1: 48 tall to touch, as every phone button is (Screens.Touch); the gap above and below is 1 where it
                    // was 3, so each row of bulls keeps the 50 it had.
                    MinHeight = Screens.Touch,
                    Margin = new Thickness(3, 1),

                    // Entry 342, the phone sweep: at 320 wide the button's own padding left a bull's number 11 points, and "10" to "25" were cut.
                    Padding = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                };
                if (on)
                {
                    button.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
                }

                string what = taps switch
                {
                    Taps.TookTwo => doubled.Contains(index) ? "took two" : "took one",
                    Taps.ForALoad => on ? "picked" : "not picked",
                    _ => on ? "fired at" : "unused",
                };
                AutomationProperties.SetName(button, $"Bull {bull.Label}, {what}" + (load is null ? "" : ", " + load));
                button.Click += (_, _) => Tap(index);
                line.Children.Add(button);
            }

            sheet.Children.Add(line);
        }

        mode.Text = taps switch
        {
            Taps.TookTwo => "Tap each bull that took two shots; tap it again for one.",
            Taps.ForALoad => "Tap the bulls to pick them, then choose their load below.",
            _ => pattern == ShotPattern.Nearest ? "" : "Tap a bull to say you fired at it, or to take it out.",
        };
        mode.IsVisible = mode.Text.Length > 0;
        unused.Text = SheetReading.UnusedLine(state);
        DrawLoads(state);
        readBack.Text = SheetReading.ReadBack(state);
    }

    private void DrawLoads(MarkingState state)
    {
        loads.Children.Clear();
        var buttons = new WrapPanel();
        foreach (var (l, words) in SheetReading.Layouts)
        {
            var button = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
            if (l == layout)
            {
                button.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
            }

            var chosen = l;
            button.Click += (_, _) => Lay(chosen);
            buttons.Children.Add(button);
        }

        loads.Children.Add(buttons);
        var map = state.Subgroups;
        string? Shared(IReadOnlyList<int> group) => group.Select(b => map?.For(b)).Distinct().Count() == 1 ? map?.For(group[0]) : null;
        switch (layout)
        {
            case LoadLayout.OneLoad:
                loads.Children.Add(List("Every bull", [.. bulls.Select(b => b.Index)], Shared([.. bulls.Select(b => b.Index)])));
                break;
            case LoadLayout.OneARow:
            case LoadLayout.OneAColumn:
                var lines = layout == LoadLayout.OneARow ? SheetReading.Rows(state.Bulls) : SheetReading.Columns(state.Bulls);
                for (int i = 0; i < lines.Count; i++)
                {
                    string name = string.Create(CultureInfo.CurrentCulture, $"{(layout == LoadLayout.OneARow ? "Row" : "Column")} {i + 1}");
                    loads.Children.Add(List(name, lines[i], Shared(lines[i])));
                }

                break;
            default:
                loads.Children.Add(Screens.Line(picked.Count == 0 ? "No bulls picked." : string.Create(CultureInfo.CurrentCulture, $"{picked.Count} bulls picked.")));
                if (picked.Count > 0)
                {
                    loads.Children.Add(List("The picked bulls", [.. picked], Shared([.. picked])));
                }

                break;
        }

        if (loadNames.Count == 1)
        {
            loads.Children.Add(Screens.Dim("No loads are in your records yet. Add one under Ballistics, The load, and it is offered here."));
        }
    }

    /// <summary>A group's name and its load, full width; choosing a load puts it on every bull of the group.</summary>
    private Control List(string name, IReadOnlyList<int> group, string? load)
    {
        var list = new ComboBox
        {
            ItemsSource = loadNames,
            SelectedIndex = Math.Max(0, loadNames.ToList().IndexOf(load ?? "No load")),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = Screens.Touch,
        };
        AutomationProperties.SetName(list, "Load for " + name.ToLowerInvariant());
        list.SelectionChanged += (_, _) =>
        {
            string? chosen = list.SelectedIndex > 0 ? list.SelectedItem as string : null;
            if (chosen != load)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => SetLoad(group, chosen));
            }
        };
        return new StackPanel { Spacing = 4, Children = { Screens.Line(name), list } };
    }
}
