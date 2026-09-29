using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 2, "tap the bulls on the sheet, A" (Alan's choice): "Bulls you fired at", reached from the result.
/// The sheet's own layout, row by row, every bull a numbered tap target of at least 44 px; a chosen bull turns amber and says so to a screen
/// reader. Chips under it: Every bull, Clear, Whole row and Whole column (the last two take one tap on a bull). A live count, then "These
/// ones" and "Not said: nearest bull for each shot". The rule is the desktop's (<see cref="AimedBulls"/>), so each shot is measured from its
/// own bull the same way on both.
/// </summary>
internal sealed class BullsPage : UserControl
{
    private enum Pick
    {
        Bull,
        Row,
        Column,
    }

    private readonly MarkingSession session;
    private readonly IReadOnlyList<BullAim> bulls;
    private readonly IReadOnlyList<IReadOnlyList<int>> rows;
    private readonly HashSet<int> chosen;
    private readonly StackPanel sheet = new() { Spacing = 8 };
    private readonly TextBlock count = Screens.Line("");
    private readonly TextBlock mode = Screens.Dim("");
    private Pick pick = Pick.Bull;

    public BullsPage(MarkingSession session, Action changed, Action back)
    {
        this.session = session;
        bulls = [.. session.State.Bulls.Where(b => b.Scoring)];
        rows = AimedBulls.Rows(bulls);
        chosen = [.. session.State.Rule is { NearestOnly: false } rule ? AimedBulls.Of(rule, bulls) : []];

        var chips = new WrapPanel
        {
            Children =
            {
                Chip("Every bull", () => { chosen.UnionWith(bulls.Select(b => b.Index)); Draw(); }),
                Chip("Clear", () => { chosen.Clear(); Draw(); }),
                Chip("Whole row", () => { pick = pick == Pick.Row ? Pick.Bull : Pick.Row; Draw(); }),
                Chip("Whole column", () => { pick = pick == Pick.Column ? Pick.Bull : Pick.Column; Draw(); }),
            },
        };
        var column = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Screens.Title("Bulls you fired at"),
                Screens.Line("Tap each bull you fired at. Tap it again to take it out."),
                Screens.Card(sheet),
                chips,
                mode,
                count,
                Screens.Primary("These ones", () =>
                {
                    session.SetAssignmentRule(chosen.Count == 0 ? null : AimedBulls.For(bulls, chosen));
                    changed();
                    back();
                }),
                Screens.Choice("Not said: nearest bull for each shot", () =>
                {
                    session.SetAssignmentRule(AssignmentRule.Nearest);
                    changed();
                    back();
                }),
            },
        };
        Content = Screens.Page(column);
        Draw();
    }

    private void Draw()
    {
        sheet.Children.Clear();
        foreach (var row in rows)
        {
            var line = new UniformGrid { Rows = 1, Columns = rows.Max(r => r.Count), HorizontalAlignment = HorizontalAlignment.Stretch };
            for (int k = 0; k < row.Count; k++)
            {
                var bull = bulls.First(b => b.Index == row[k]);
                int place = k;
                bool on = chosen.Contains(bull.Index);
                var button = new Button
                {
                    Content = bull.Label,
                    MinWidth = 44,
                    MinHeight = 44,
                    Margin = new Thickness(3),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                };
                if (on)
                {
                    button.Classes.Add(GroupLab.App.Theme.AppStyles.Chosen);
                }

                AutomationProperties.SetName(button, $"Bull {bull.Label}, {(on ? "chosen" : "not chosen")}");
                button.Click += (_, _) => Choose(bull.Index, row, place);
                line.Children.Add(button);
            }

            sheet.Children.Add(line);
        }

        int shots = session.State.Shots.Count(s => s.IsShot);
        count.Text = string.Create(CultureInfo.CurrentCulture, $"{chosen.Count} of {bulls.Count} bulls chosen, {shots} shot{(shots == 1 ? "" : "s")}");
        mode.Text = pick switch
        {
            Pick.Row => "Tap a bull to choose its whole row.",
            Pick.Column => "Tap a bull to choose its whole column.",
            _ => "",
        };
        mode.IsVisible = mode.Text.Length > 0;
    }

    private void Choose(int index, IReadOnlyList<int> row, int place)
    {
        switch (pick)
        {
            case Pick.Row:
                chosen.UnionWith(row);
                break;
            case Pick.Column:
                chosen.UnionWith(rows.Where(r => r.Count > place).Select(r => r[place]));
                break;
            default:
                if (!chosen.Remove(index))
                {
                    chosen.Add(index);
                }

                break;
        }

        pick = Pick.Bull;
        Draw();
    }

    private static Button Chip(string words, Action chosen)
    {
        var chip = new Button { Content = words, MinHeight = Screens.Touch, Margin = new Thickness(0, 0, 6, 6) };
        chip.Click += (_, _) => chosen();
        return chip;
    }
}
