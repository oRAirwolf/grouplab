using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// A load per bull, NOTES-FROM-PLANNING.md entry 115 section 2, which answers question 27. A ladder sheet is usually five bulls to a load, so
/// the bulls are chosen on the sheet, with shift or control to take more than one, and the load is set on all of them at once. The load names
/// come from the records, so a load named twice is not two loads. The bulls sharing a load are a subgroup, which the analysis compares as it
/// already can, and a bull with no load belongs to none: the panel says how many are unset rather than inventing a default. It is kept in the
/// marking, so a session carries its subgroups and a reopened one still has them.
/// </summary>
public sealed partial class MainWindow
{
    private readonly ComboBox bullLoad = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel bullLoadLines = new() { Spacing = 2 };
    private HashSet<int> bullSelection = [];

    private void BuildBullLoads(StackPanel panel)
    {
        panel.Children.Add(FieldLabel("Load on the chosen bulls"));
        panel.Children.Add(bullLoad);
        panel.Children.Add(Row(Button("Set", () => SetLoadOnChosenBulls(bullLoad.SelectedIndex > 0 ? bullLoad.SelectedItem as string : null)), Button("Clear", () => SetLoadOnChosenBulls(null))));
        panel.Children.Add(bullLoadLines);
        canvas.BullClicked += (_, chosen) =>
        {
            if (!chosen.Add)
            {
                bullSelection = bullSelection.Contains(chosen.Bull) && bullSelection.Count == 1 ? [] : [chosen.Bull];
            }
            else if (!bullSelection.Add(chosen.Bull))
            {
                bullSelection.Remove(chosen.Bull);
            }

            Refresh();
        };
    }

    /// <summary>Puts the chosen bulls in a load's subgroup, or takes them out of one, and keeps the marking.</summary>
    internal void SetLoadOnChosenBulls(string? load)
    {
        if (bullSelection.Count == 0)
        {
            problem.Text = "Choose the bulls first: with the select tool, hold shift and click each bull on the sheet.";
            return;
        }

        foreach (int bull in bullSelection)
        {
            session.SetSubgroup(bull, load);
        }

        DiagnosticLog.Info("marking.subgroup", ("bulls", bullSelection.Count), ("set", load is not null));
        status.Text = load is null
            ? string.Create(CultureInfo.InvariantCulture, $"{bullSelection.Count} bulls have no load now.")
            : string.Create(CultureInfo.InvariantCulture, $"{load} on {bullSelection.Count} bulls.");
        Refresh();
    }

    /// <summary>The load field and what it says: the loads from the records, the bulls chosen, and how many carry no load.</summary>
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
        bullLoadLines.Children.Clear();
        var scoring = state.Bulls.Where(b => b.Scoring).ToList();
        if (scoring.Count == 0)
        {
            bullLoadLines.Children.Add(Line("This marking has no bulls, so there is nothing to put a load on."));
            return;
        }

        bullLoadLines.Children.Add(Line(bullSelection.Count switch
        {
            0 => "No bulls chosen. With the select tool, hold shift and click the bulls on the sheet; shift and click again takes one back.",
            1 => $"Bull {BullLabel(bullSelection.First())} chosen.",
            _ => string.Create(CultureInfo.InvariantCulture, $"{bullSelection.Count} bulls chosen: {string.Join(", ", bullSelection.Order().Select(BullLabel))}."),
        }));

        var map = state.Subgroups;
        int unset = scoring.Count(b => map?.For(b.Index) is null);
        foreach (string name in map?.Names ?? [])
        {
            int count = scoring.Count(b => map!.For(b.Index) == name);
            bullLoadLines.Children.Add(Detail(string.Create(CultureInfo.InvariantCulture, $"{name}: {count} bull{(count == 1 ? "" : "s")}")));
        }

        bullLoadLines.Children.Add(Detail(unset == 0
            ? "Every bull carries a load."
            : string.Create(CultureInfo.InvariantCulture, $"{unset} of {scoring.Count} bulls carry no load, and belong to no subgroup.")));
    }

    /// <summary>Chooses bulls as clicks on the sheet do, for the headless tests.</summary>
    internal void ChooseBulls(params int[] bulls)
    {
        bullSelection = [.. bulls];
        Refresh();
    }

    internal IReadOnlySet<int> ChosenBulls => bullSelection;

    /// <summary>Picks a load in the field by name, for the headless tests.</summary>
    internal void PickBullLoad(string? load)
    {
        bullLoad.SelectedIndex = load is null ? 0 : Math.Max(0, bullLoad.ItemsSource!.Cast<string>().ToList().IndexOf(load));
    }

    internal IEnumerable<string> BullLoadText => bullLoadLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
