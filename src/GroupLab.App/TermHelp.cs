using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 154 section 3: a label or heading that names a glossary word explains it, with the same words the website
/// uses, read from the one list in <c>glossary.json</c>. The label itself is the affordance, drawn with a dotted underline as the website
/// draws it: the plain sentence on hover, and from the keyboard the label takes focus with Tab, shows the sentence while it has it, and
/// opens the whole entry on Enter, as a tap or click does.
/// </summary>
internal static class TermHelp
{
    public const string Class = "term";

    /// <summary>Which labels are looked at: the figure labels, the section headings and the column headings the screens are built from.</summary>
    private static readonly string[] Labels = [AppStyles.Label, AppStyles.Section, AppStyles.Dim];

    /// <summary>Explains a label in place where it names a glossary word, and returns it.</summary>
    public static TextBlock Explain(TextBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (block.Classes.Contains(Class) || Glossary.Find(block.Text ?? "") is not { } term)
        {
            return block;
        }

        block.Classes.Add(Class);
        block.Tag = term.Term;
        block.TextDecorations =
        [
            new TextDecoration { Location = TextDecorationLocation.Underline, StrokeDashArray = new AvaloniaList<double> { 1, 2 }, StrokeThickness = 1 },
        ];
        block.Cursor = new Cursor(StandardCursorType.Help);
        block.Focusable = true;
        ToolTip.SetTip(block, term.Plain);
        Avalonia.Automation.AutomationProperties.SetHelpText(block, term.Plain);
        FlyoutBase.SetAttachedFlyout(block, new Flyout { Content = Entry(term), Placement = PlacementMode.Bottom });
        block.GotFocus += (_, _) => ToolTip.SetIsOpen(block, true);
        block.LostFocus += (_, _) => ToolTip.SetIsOpen(block, false);
        block.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                FlyoutBase.ShowAttachedFlyout(block);
                e.Handled = true;
            }
        };
        block.Tapped += (_, _) => FlyoutBase.ShowAttachedFlyout(block);
        return block;
    }

    /// <summary>Every label and heading under a control that names a glossary word, explained. Run after a screen is built or refreshed.</summary>
    public static void ExplainAll(ILogical root)
    {
        ArgumentNullException.ThrowIfNull(root);
        foreach (var block in root.GetLogicalDescendants().OfType<TextBlock>())
        {
            if (!block.Classes.Contains(Class) && Labels.Any(block.Classes.Contains))
            {
                Explain(block);
            }
        }
    }

    /// <summary>The whole entry: the plain sentence, the precise one where there is one, and the way to the glossary page.</summary>
    private static Control Entry(GlossaryTerm term)
    {
        var panel = new StackPanel { Spacing = Tokens.Space8, MaxWidth = 360 };
        panel.Children.Add(new TextBlock { Text = term.Name, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock { Text = term.Plain, TextWrapping = TextWrapping.Wrap });
        if (term.Precise is { } precise)
        {
            panel.Children.Add(new TextBlock { Text = "Precisely: " + precise, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        }

        var more = new Button { Content = "More in the glossary", HorizontalAlignment = HorizontalAlignment.Left, Classes = { AppStyles.Link } };
        more.Click += (_, _) => TheOutsideWorld.Current.OpenAddress(Glossary.MoreAbout(term));
        panel.Children.Add(more);
        return panel;
    }
}
