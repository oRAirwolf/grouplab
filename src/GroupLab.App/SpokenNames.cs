using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1: a button, choice or list row whose content is a panel of words and pictures is named by
/// Avalonia after the panel's type, so Narrator read the printer check's four ways to print as "Avalonia.Controls.StackPanel". Each one
/// without a name of its own takes the words inside it, joined by commas, and keeps them up to date when its content changes. A name the
/// code gave it is never replaced.
/// </summary>
internal static class SpokenNames
{
    private static readonly ConditionalWeakTable<ContentControl, string> Given = new();
    private static bool attached;

    public static void Attach()
    {
        if (attached)
        {
            return;
        }

        attached = true;
        Control.LoadedEvent.AddClassHandler<ContentControl>((c, _) => Fill(c));
        ContentControl.ContentProperty.Changed.AddClassHandler<ContentControl>((c, _) =>
        {
            if (c.IsLoaded)
            {
                Fill(c);
            }
        });
    }

    public static void Fill(ContentControl c)
    {
        if (c is not (Button or ListBoxItem or ComboBoxItem) || AutomationProperties.GetLabeledBy(c) is not null)
        {
            return;
        }

        string? name = AutomationProperties.GetName(c);
        bool ours = Given.TryGetValue(c, out string? given) && name == given;
        if (!string.IsNullOrEmpty(name) && !ours)
        {
            return;
        }

        string words = c.Content is Control inside ? WordsIn(inside) : "";
        if (words.Length == 0)
        {
            if (ours)
            {
                c.ClearValue(AutomationProperties.NameProperty);
                Given.Remove(c);
            }

            return;
        }

        AutomationProperties.SetName(c, words);
        Given.AddOrUpdate(c, words);
    }

    /// <summary>The words a person sees inside, in reading order: every text block's text, and a nested control's own spoken name.</summary>
    internal static string WordsIn(Control root)
    {
        var words = new List<string>();
        Collect(root, words);
        return string.Join(", ", words);
    }

    private static void Collect(ILogical node, List<string> words)
    {
        if (node is Control { IsVisible: false })
        {
            return;
        }

        if (node is TextBlock block)
        {
            string text = !string.IsNullOrWhiteSpace(block.Text) ? block.Text : string.Concat(block.Inlines?.OfType<Run>().Select(r => r.Text) ?? []);
            if (!string.IsNullOrWhiteSpace(text))
            {
                words.Add(text.Trim());
            }

            return;
        }

        if (node is Control control && AutomationProperties.GetName(control) is { Length: > 0 } named)
        {
            words.Add(named);
            return;
        }

        foreach (var child in node.LogicalChildren)
        {
            Collect(child, words);
        }
    }
}
