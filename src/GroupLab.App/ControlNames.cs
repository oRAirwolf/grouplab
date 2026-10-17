using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace GroupLab.App;

/// <summary>
/// One way of naming a control, NOTES-FROM-PLANNING.md entries 437 and 439: the user guide's pictures record their controls' places by these
/// names, and the guided demo finds the control it points at by them, so a renamed button breaks a test or the site build rather than a
/// marking or a tour step. A control's name is its <c>Name</c>, else its accessible name, else the words on it (a button, tab, check box or
/// menu entry). A part of a control's template has no name of its own: the control it belongs to is the thing a reader looks for.
/// </summary>
internal static partial class ControlNames
{
    public static string? Of(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (control.TemplatedParent is not null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(control.Name))
        {
            return control.Name;
        }

        if (AutomationProperties.GetName(control) is { Length: > 0 } spoken)
        {
            return Words(spoken);
        }

        object? content = control switch
        {
            TabItem tab => tab.Header,
            MenuItem item => item.Header,
            Button or ToggleButton => ((ContentControl)control).Content,
            _ => null,
        };
        string? text = content switch
        {
            string s => s,
            TextBlock block => block.Text,
            Control inner => inner.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(text) ? null : Words(text);
    }

    /// <summary>The first control on show under <paramref name="root"/> with this name, or none.</summary>
    public static Control? Find(Visual root, string name)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsEffectivelyVisible && c.Opacity > 0 && Of(c) == name);
    }

    private static string Words(string text) => Space().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Space();
}
