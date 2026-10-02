using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker A item 2: the phone sweep found some forty fields and switches a screen reader announced only as
/// "text field" or "switch", because their words were a label beside them rather than their own name. Every field and switch on every screen
/// now takes the words a person reads for it, once it is on screen: its own automation name where it has one, else its placeholder, else
/// the nearest label before it in the panels that hold it. Done in one place so a field added tomorrow is named the same way.
/// </summary>
internal static class FieldNames
{
    private static bool attached;

    /// <summary>How many panels up a label is looked for: a field's own row, its labelled pair, and the card around them.</summary>
    private const int Reach = 3;

    public static void Attach()
    {
        if (attached)
        {
            return;
        }

        attached = true;
        Control.LoadedEvent.AddClassHandler<TextBox>((field, _) => Name(field));
        Control.LoadedEvent.AddClassHandler<ToggleSwitch>((toggle, _) => Name(toggle));
        Control.LoadedEvent.AddClassHandler<ComboBox>((box, _) => Name(box));

        // A button whose content is a panel (an icon over a word, a row of words) was announced by the panel's type, "Avalonia.Controls.
        // StackPanel": the places along the bottom among them. It takes the words inside it.
        Control.LoadedEvent.AddClassHandler<Button>((button, _) => Words(button));
        Control.LoadedEvent.AddClassHandler<ToggleButton>((button, _) => Words(button));
    }

    /// <summary>A button holding a panel, named by the words in it, where it has no name of its own.</summary>
    internal static void Words(ContentControl button)
    {
        if (button.Content is not (Panel or Decorator) || !string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))
        {
            return;
        }

        var words = ((Control)button.Content).GetLogicalDescendants().OfType<TextBlock>()
            .Select(t => t.Text?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Take(3).ToList();
        if (words.Count > 0)
        {
            AutomationProperties.SetName(button, string.Join(", ", words));
        }
    }

    /// <summary>Names a control that has no name of its own from the words a person reads for it; nothing where there are none.</summary>
    internal static void Name(Control control)
    {
        if (!string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
        {
            return;
        }

        string? words = control switch
        {
            TextBox { PlaceholderText.Length: > 0 } field => field.PlaceholderText,
            ToggleButton { Content: string said } when said.Length > 0 => said,
            _ => null,
        } ?? Label(control);
        if (!string.IsNullOrWhiteSpace(words))
        {
            AutomationProperties.SetName(control, words.Trim());
        }
    }

    /// <summary>The nearest words before a control: a text before it in its panel, or the last words of a panel before it, up to <see cref="Reach"/> panels out.</summary>
    internal static string? Label(Control control)
    {
        int levels = 0;
        for (Control? at = control; at?.Parent is Panel panel && levels < Reach; at = panel, levels++)
        {
            int i = panel.Children.IndexOf(at);
            foreach (var before in panel.Children.Take(Math.Max(0, i)).Reverse())
            {
                string? text = before is TextBlock own ? own.Text
                    : before.GetLogicalDescendants().OfType<TextBlock>().LastOrDefault(t => !string.IsNullOrWhiteSpace(t.Text))?.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }
}
