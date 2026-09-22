using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
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
    private readonly StackPanel bullLoadLines = new() { Spacing = Tokens.Space4 };
    private readonly StackPanel aimedAtLines = new() { Spacing = Tokens.Space4 };
    private HashSet<int> bullSelection = [];

    private void BuildBullLoads(StackPanel panel)
    {
        panel.Children.Add(FieldLabel("Load on the chosen bulls"));
        panel.Children.Add(bullLoad);
        panel.Children.Add(Row(Button("Set", () => SetLoadOnChosenBulls(bullLoad.SelectedIndex > 0 ? bullLoad.SelectedItem as string : null)), Button("Clear", () => SetLoadOnChosenBulls(null))));
        panel.Children.Add(bullLoadLines);

        // Question 37 and entry 130 section 3.1. The offset solver is the fix for the worst defect this project has found, and it only runs
        // where the shooter has said which bulls they aimed at, because a sheet of twenty five bulls with ten shot has a translation that
        // explains the holes for almost any reading. Until this control existed there was no way to say it, so the fix was in the build and
        // out of reach. The bulls are chosen the same way a load's are: click one, shift-click for more.
        panel.Children.Add(FieldLabel("Bulls you fired at"));
        panel.Children.Add(Row(
            Button("These ones", () => SetAimedAtChosenBulls(false)),
            Button("Every bull", () => SetAimedAtChosenBulls(true)),
            Button("Clear", () => SetAimedAt(null))));
        panel.Children.Add(aimedAtLines);

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

    /// <summary>
    /// Says which bulls the shooter fired at, so the offset solver can run (question 37, entry 130 section 3.1). Every bull takes one shot,
    /// which is what a sheet of bulls means unless the doubles rule says otherwise.
    /// </summary>
    internal void SetAimedAtChosenBulls(bool everyBull)
    {
        var bulls = everyBull
            ? session.State.Bulls.Where(b => b.Scoring).Select(b => b.Index).ToList()
            : [.. bullSelection];

        if (bulls.Count == 0)
        {
            problem.Text = "Choose the bulls first: with the select tool, click one and hold shift to add more. Or press Every bull.";
            return;
        }

        SetAimedAt(new AssignmentRule(false, bulls.ToImmutableDictionary(b => b, _ => 1)));
    }

    /// <summary>Sets or clears the rule, and says what it did.</summary>
    private void SetAimedAt(AssignmentRule? rule)
    {
        session.SetAssignmentRule(rule);
        DiagnosticLog.Info("marking.aimed", ("bulls", rule?.PerBull.Count ?? 0));
        toaster.Show(new Confirmation(
            rule is null
                ? "Cleared which bulls you fired at."
                : string.Create(CultureInfo.InvariantCulture, $"{rule.PerBull.Count} bull{(rule.PerBull.Count == 1 ? "" : "s")} marked as fired at."),
            session.CanUndo ? () => session.Undo() : null));
        Refresh();
    }

    /// <summary>What the panel says about the rule, rebuilt on every refresh.</summary>
    private void ShowAimedAt(MarkingState state)
    {
        aimedAtLines.Children.Clear();
        if (state.Bulls.Count == 0)
        {
            aimedAtLines.Children.Add(Line("This marking has no bulls, so there is nothing to say which of them you shot at."));
            return;
        }

        if (state.Rule is not { } rule || rule.NearestOnly || rule.PerBull.IsEmpty)
        {
            aimedAtLines.Children.Add(Line("Not said. Where a group lands away from where it was aimed, GroupLab cannot tell which bulls you meant to hit, so it measures each shot from whichever bull it landed nearest. Saying which bulls you fired at lets it work out where the group actually landed and measure from the right ones."));
            return;
        }

        aimedAtLines.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
            $"{rule.PerBull.Count} of {state.Bulls.Count(b => b.Scoring)} bulls, one shot each. Shots are assigned in the frame the group actually landed in.")));
    }

    /// <summary>What the panel says about which bulls were fired at, for the headless tests.</summary>
    internal IReadOnlyList<string> AimedAtText => [.. aimedAtLines.Children.OfType<TextBlock>().Select(t => t.Text ?? "")];

    /// <summary>Picks a load in the field by name, for the headless tests.</summary>
    internal void PickBullLoad(string? load)
    {
        bullLoad.SelectedIndex = load is null ? 0 : Math.Max(0, bullLoad.ItemsSource!.Cast<string>().ToList().IndexOf(load));
    }

    internal IEnumerable<string> BullLoadText => bullLoadLines.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");
}
