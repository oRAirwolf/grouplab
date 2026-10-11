using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// A load per bull, NOTES-FROM-PLANNING.md entry 115 section 2, which answers question 27. The bulls sharing a load are a subgroup, which the
/// analysis compares as it already can, and a bull with no load belongs to none. It is kept in the marking, so a session carries its subgroups.
/// <para>
/// Entry 407 section 2.2 (concept 1A): "Loads on this sheet", four buttons. One load puts one load on every bull; One a row and One a column
/// show a short list of rows or columns, each with its load, and the sheet shows the load beside each; I will pick bulls is what this was
/// before, bulls chosen on the sheet with shift and a load set on them. "Its row" and "Its column" are the two middle buttons now.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly ComboBox bullLoad = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel bullLoadLines = new() { Spacing = Tokens.Space4 };
    private readonly StackPanel aimedAtLines = new() { Spacing = Tokens.Space4 };
    private readonly TextBlock aimHint = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly StackPanel loadsBody = new() { Spacing = Tokens.Space4 };
    private readonly StackPanel pickBulls = new() { Spacing = Tokens.Space4 };
    private readonly List<(LoadLayout Layout, ToggleButton Button)> layoutButtons = [];
    private HashSet<int> bullSelection = [];
    private LoadLayout? layoutChosen;

    private void BuildBullLoads(StackPanel panel)
    {
        panel.Children.Add(FieldLabel("Loads on this sheet"));
        var buttons = new WrapPanel();
        foreach (var (layout, words) in SheetReading.Layouts)
        {
            var button = new ToggleButton { Content = words, Margin = new Avalonia.Thickness(0, 0, Tokens.Space4, Tokens.Space4) };
            var chosen = layout;
            button.Click += (_, _) => ChooseLoadLayout(chosen);
            layoutButtons.Add((layout, button));
            buttons.Children.Add(button);
        }

        panel.Children.Add(buttons);
        panel.Children.Add(loadsBody);

        // I will pick bulls: the bulls chosen on the sheet with shift, and a load set on them (entry 115 section 2).
        pickBulls.Children.Add(FieldLabel("Load on the chosen bulls"));
        pickBulls.Children.Add(bullLoad);
        pickBulls.Children.Add(Row(Button("Set", () => SetLoadOnChosenBulls(bullLoad.SelectedIndex > 0 ? bullLoad.SelectedItem as string : null)), Button("Clear", () => SetLoadOnChosenBulls(null))));
        panel.Children.Add(ruleSays);

        canvas.BullClicked += (_, chosen) =>
        {
            // Entry 407 section 1.3: a plain click on a bull chooses it to give holes to; only shift or the command key picks bulls for a load.
            if (!chosen.Add)
            {
                return;
            }

            if (!bullSelection.Add(chosen.Bull))
            {
                bullSelection.Remove(chosen.Bull);
            }

            if (layoutChosen != LoadLayout.PickBulls)
            {
                layoutChosen = LoadLayout.PickBulls;
            }

            Refresh();
        };
    }

    /// <summary>Chooses how the loads are laid out on the sheet, and shows what that layout asks for.</summary>
    internal void ChooseLoadLayout(LoadLayout layout)
    {
        layoutChosen = layout;
        DiagnosticLog.Info("marking.load-layout", ("layout", layout.ToString()));
        ShowBullLoads(session.State);
    }

    /// <summary>Puts the chosen bulls in a load's subgroup, or takes them out of one, and keeps the marking.</summary>
    internal void SetLoadOnChosenBulls(string? load)
    {
        if (bullSelection.Count == 0)
        {
            problem.Text = "Choose the bulls first: hold shift and click each bull on the sheet.";
            return;
        }

        SheetReading.SetLoad(session, bullSelection, load);
        DiagnosticLog.Info("marking.subgroup", ("bulls", bullSelection.Count), ("set", load is not null));
        status.Text = load is null
            ? string.Create(CultureInfo.InvariantCulture, $"{bullSelection.Count} bulls have no load now.")
            : string.Create(CultureInfo.InvariantCulture, $"{load} on {bullSelection.Count} bulls.");
        Refresh();
    }

    /// <summary>Puts one load on a group of bulls, a whole row, a whole column or the whole sheet, from that group's list.</summary>
    internal void SetLoadOn(IReadOnlyList<int> bulls, string? load)
    {
        SheetReading.SetLoad(session, bulls, load);
        DiagnosticLog.Info("marking.subgroup", ("bulls", bulls.Count), ("set", load is not null));
    }

    /// <summary>The loads part and what it says: the layout, its lists, the bulls chosen, and how many carry no load.</summary>
    private void ShowBullLoads(MarkingState state)
    {
        var loads = new[] { "No load" }.Concat(book.Loads.Select(l => l.Name)).ToList();
        if (!bullLoad.ItemsSource?.Cast<string>().SequenceEqual(loads) ?? true)
        {
            string? held = bullLoad.SelectedItem as string;
            bullLoad.ItemsSource = loads;
            bullLoad.SelectedIndex = Math.Max(0, loads.IndexOf(held ?? "No load"));
        }

        bullSelection.RemoveWhere(b => state.Bulls.All(x => x.Index != b));
        canvas.SelectedBulls = bullSelection;
        var layout = layoutChosen ?? SheetReading.LayoutOf(state);
        foreach (var (l, button) in layoutButtons)
        {
            button.IsChecked = l == layout;
        }

        loadsBody.Children.Clear();
        bullLoadLines.Children.Clear();
        canvas.SideLabels = [];
        var scoring = state.Bulls.Where(b => b.Scoring).ToList();
        if (scoring.Count == 0)
        {
            loadsBody.Children.Add(Line("This marking has no bulls, so there is nothing to put a load on."));
            return;
        }

        var map = state.Subgroups;
        switch (layout)
        {
            case LoadLayout.OneLoad:
                string? all = scoring.Select(b => map?.For(b.Index)).Distinct().Count() == 1 ? map?.For(scoring[0].Index) : null;
                loadsBody.Children.Add(LoadList("Every bull", [.. scoring.Select(b => b.Index)], all, loads));
                break;
            case LoadLayout.OneARow:
            case LoadLayout.OneAColumn:
                bool byRow = layout == LoadLayout.OneARow;
                var lines = byRow ? SheetReading.Rows(state.Bulls) : SheetReading.Columns(state.Bulls);
                var labels = new List<(PointD, string)>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    string? load = line.Select(b => map?.For(b)).Distinct().Count() == 1 ? map?.For(line[0]) : null;
                    loadsBody.Children.Add(LoadList(string.Create(CultureInfo.InvariantCulture, $"{(byRow ? "Row" : "Column")} {i + 1}"), line, load, loads));
                    if (load is not null && state.Bulls.FirstOrDefault(b => b.Index == line[0]) is { } first)
                    {
                        labels.Add((Beside(state, first, byRow), load));
                    }
                }

                canvas.SideLabels = labels;
                break;
            default:
                loadsBody.Children.Add(pickBulls);
                bullLoadLines.Children.Add(Line(bullSelection.Count switch
                {
                    0 => "No bulls chosen. Hold shift and click the bulls on the sheet; shift and click again takes one back.",
                    1 => $"Bull {BullLabel(bullSelection.First())} chosen.",
                    _ => string.Create(CultureInfo.InvariantCulture, $"{bullSelection.Count} bulls chosen: {string.Join(", ", bullSelection.Order().Select(BullLabel))}."),
                }));
                break;
        }

        int unset = scoring.Count(b => map?.For(b.Index) is null);
        foreach (string name in map?.Names ?? [])
        {
            int count = scoring.Count(b => map!.For(b.Index) == name);
            bullLoadLines.Children.Add(Detail(string.Create(CultureInfo.InvariantCulture, $"{name}: {count} bull{(count == 1 ? "" : "s")}")));
        }

        bullLoadLines.Children.Add(Detail(unset == 0
            ? "Every bull carries a load."
            : string.Create(CultureInfo.InvariantCulture, $"{unset} of {scoring.Count} bulls carry no load, and belong to no subgroup.")));
        loadsBody.Children.Add(bullLoadLines);

        ruleSays.Text = SheetReading.ReadBack(state);
    }

    /// <summary>One group's line: its name and a list of the loads, which sets the load on every bull of the group when chosen.</summary>
    private Control LoadList(string name, IReadOnlyList<int> bulls, string? load, IReadOnlyList<string> loads)
    {
        var list = new ComboBox { ItemsSource = loads, SelectedIndex = Math.Max(0, loads.ToList().IndexOf(load ?? "No load")), MinWidth = 160 };
        Avalonia.Automation.AutomationProperties.SetName(list, "Load for " + name.ToLowerInvariant());
        list.SelectionChanged += (_, _) =>
        {
            string? chosen = list.SelectedIndex > 0 ? list.SelectedItem as string : null;
            if (chosen != load)
            {
                SetLoadOn(bulls, chosen);
            }
        };
        var label = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center, MinWidth = 80 };
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, Children = { label, list } };
    }

    /// <summary>Where a row's load is written, left of its first bull, or a column's, above its top bull: half a bull's spacing away.</summary>
    private static PointD Beside(MarkingState state, BullAim first, bool byRow)
    {
        double spacing = state.Bulls.Where(b => b.Index != first.Index)
            .Select(b => Math.Sqrt(Math.Pow(b.Image.X - first.Image.X, 2) + Math.Pow(b.Image.Y - first.Image.Y, 2))).DefaultIfEmpty(100).Min();
        return byRow ? new PointD(first.Image.X - (0.9 * spacing), first.Image.Y) : new PointD(first.Image.X, first.Image.Y - (0.6 * spacing));
    }

    /// <summary>Chooses bulls as shift-clicks on the sheet do, for the headless tests.</summary>
    internal void ChooseBulls(params int[] bulls)
    {
        bullSelection = [.. bulls];
        layoutChosen = LoadLayout.PickBulls;
        Refresh();
    }

    internal IReadOnlySet<int> ChosenBulls => bullSelection;

    /// <summary>
    /// Entry 187 section 6, question 50's option 2, quietly: a hint where the sheet has more scoring bulls than shots, never a review item, never
    /// holding Accept back, and never claiming anything would move. Answering it or putting it away once is final for the target. Entry 407
    /// section 2 points it at the bulls unused, which is where the answer is given now.
    /// </summary>
    private void ShowAimedAt(MarkingState state)
    {
        ShowRule();
        aimedAtLines.Children.Clear();
        if (state.Bulls.Count == 0)
        {
            return;
        }

        if (SheetReading.FiredAt(state.Rule, state.Bulls) is null && state.Rule is not { NearestOnly: true })
        {
            int scoring = state.Bulls.Count(b => b.Scoring);
            int shots = state.ExpectedShots ?? state.Shots.Count(s => s.IsShot);
            if (shots > 0 && shots < scoring && state.ImagePath is { } target && !settingsStore.AimHintPutAway(target))
            {
                aimHint.Text = string.Create(CultureInfo.InvariantCulture,
                    $"This sheet has {scoring} bulls and {shots} shots. If you fired at only some of the bulls, check the bulls unused and tap any GroupLab has wrong.");
                aimedAtLines.Children.Add(aimHint);
                aimedAtLines.Children.Add(Button("Put this away", () =>
                {
                    settingsStore.PutAwayAimHint(target);
                    DiagnosticLog.Info("marking.aim-hint", ("put-away", true));
                    ShowAimedAt(session.State);
                }));
            }

            return;
        }

        // Saying which bulls answers the hint for this target for good.
        if (state.ImagePath is { } answered && !settingsStore.AimHintPutAway(answered))
        {
            settingsStore.PutAwayAimHint(answered);
        }
    }

    /// <summary>What the panel says about which bulls were fired at, for the headless tests.</summary>
    internal IReadOnlyList<string> AimedAtText => [.. aimedAtLines.Children.OfType<TextBlock>().Select(t => t.Text ?? ""), UnusedText];

    /// <summary>Presses the hint's Put this away, for the headless tests.</summary>
    internal void PutAwayAimHintNow() => aimedAtLines.Children.OfType<Avalonia.Controls.Button>().First(b => b.Content as string == "Put this away")
        .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

    /// <summary>Picks a load in the field by name, for the headless tests.</summary>
    internal void PickBullLoad(string? load)
    {
        bullLoad.SelectedIndex = load is null ? 0 : Math.Max(0, bullLoad.ItemsSource!.Cast<string>().ToList().IndexOf(load));
    }

    internal IEnumerable<string> BullLoadText => loadsBody.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
