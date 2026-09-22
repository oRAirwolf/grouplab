using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// How a sheet's shots are read against its bulls, NOTES-FROM-PLANNING.md entry 113 section 4: one a bull, matched, which is the default; every
/// shot to its nearest bull; or two on the bulls named, as the doubles sheet is shot. Without it, a sheet that breaks one a bull on purpose has
/// each second shot pushed onto an empty neighbour, and the review queue raises every one.
/// </summary>
public sealed partial class MainWindow
{
    private readonly ComboBox shotsPerBull = new()
    {
        ItemsSource = new[] { "One a bull, matched", "Nearest bull, however many", "Two on the bulls named", "Only the bulls I aimed at" },
        SelectedIndex = 0,
        HorizontalAlignment = HorizontalAlignment.Stretch,
    };

    private readonly TextBox doubledBulls = new() { PlaceholderText = "bulls, such as 1-10, rows 1-3, columns 2-5", HorizontalAlignment = HorizontalAlignment.Stretch };

    /// <summary>What the rule in force says, read back to the person: entry 141 section 5.3.4.</summary>
    private readonly TextBlock ruleSays = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    private void BuildShotsPerBull(StackPanel panel)
    {
        panel.Children.Add(FieldLabel("Shots per bull"));
        panel.Children.Add(shotsPerBull);
        panel.Children.Add(doubledBulls);
        panel.Children.Add(Row(Button("Apply", ApplyShotsPerBull)));
        panel.Children.Add(ruleSays);
        Avalonia.Automation.AutomationProperties.SetName(doubledBulls, "The bulls holding two shots, or the bulls aimed at");
    }

    /// <summary>Reads the choice and the bulls named, and has the marking read that way.</summary>
    internal void ApplyShotsPerBull()
    {
        switch (shotsPerBull.SelectedIndex)
        {
            case 1:
                session.SetAssignmentRule(AssignmentRule.Nearest);
                status.Text = "Every shot goes to its nearest bull, and a bull holding more than one is not raised.";
                break;
            case 2:
                if (NamedBulls(doubledBulls.Text ?? "") is not { Count: > 0 } named)
                {
                    problem.Text = "Name the bulls holding two shots by their numbers, such as 1-10 or 1, 3, 5.";
                    return;
                }

                session.SetAssignmentRule(new AssignmentRule(false, named.ToImmutableDictionary(b => b, _ => 2)));
                status.Text = string.Create(CultureInfo.InvariantCulture, $"Matched with {named.Count} bulls taking two shots each.");
                break;
            case 3:
                // Entry 141 section 5.3.4 and question 37: the one thing the shooter knows and the sheet cannot show. A bull nobody aimed at
                // expects no shots, which closes it to the matching, so a sheet shot with the wrong zero stops handing every shot to the bull
                // it happens to have landed nearest.
                if (AimedFromBox() is not { } aimed)
                {
                    problem.Text = "Name the bulls you aimed at: 1-15, or rows 1-3, or columns 2-5 for the same columns of every row.";
                    return;
                }

                session.SetAssignmentRule(aimed);
                status.Text = AimedBulls.Says(aimed, session.State.Bulls);
                break;
            default:
                session.SetAssignmentRule(null);
                status.Text = "One shot a bull, matched.";
                break;
        }

        ShowRule();

        DiagnosticLog.Info("assignment.rule", ("choice", shotsPerBull.SelectedIndex));
    }

    /// <summary>
    /// The rule the box describes, or null where it describes nothing. Three ways of saying it, because three ways is how people shoot:
    /// a list of bulls, whole rows, or the same columns of every row.
    /// </summary>
    private AssignmentRule? AimedFromBox() => AimedBulls.Parse(doubledBulls.Text, session.State.Bulls);

    /// <summary>Writes the rule in force under the control, so a person can check what they told GroupLab.</summary>
    private void ShowRule() => ruleSays.Text = session.State.Bulls.Count == 0 ? "" : AimedBulls.Says(session.State.Rule, session.State.Bulls);

    /// <summary>The bulls a list like "1-10, 12" names, by their printed labels, as indices; null when any part names no bull.</summary>
    private List<int>? NamedBulls(string text)
    {
        var byLabel = session.State.Bulls.Where(b => b.Scoring).GroupBy(b => b.Label).ToDictionary(g => g.Key, g => g.First().Index);
        var named = new List<int>();
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] ends = part.Split('-', StringSplitOptions.TrimEntries);
            if (ends.Length == 2 && int.TryParse(ends[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int from)
                && int.TryParse(ends[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int to) && from <= to)
            {
                for (int n = from; n <= to; n++)
                {
                    if (!byLabel.TryGetValue(n.ToString(CultureInfo.InvariantCulture), out int index))
                    {
                        return null;
                    }

                    named.Add(index);
                }
            }
            else if (byLabel.TryGetValue(part, out int index))
            {
                named.Add(index);
            }
            else
            {
                return null;
            }
        }

        return [.. named.Distinct()];
    }

    /// <summary>Sets the choice and the bulls as a person would, and applies them, for the headless tests.</summary>
    internal void SetShotsPerBull(int choice, string bulls)
    {
        shotsPerBull.SelectedIndex = choice;
        doubledBulls.Text = bulls;
        ApplyShotsPerBull();
    }
}
