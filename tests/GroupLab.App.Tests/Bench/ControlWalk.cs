using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using GroupLab.App;

namespace GroupLab.App.Tests.Bench;

/// <summary>What a person can click, found by walking the window rather than by a list somebody maintains (entry 117 section 3b).</summary>
/// <param name="Screen">The destination it was found on.</param>
/// <param name="Label">What it says, or its name where it says nothing: the key the record and the exclusion list use.</param>
/// <param name="Kind">Button, toggle, tab, disclosure, dropdown or row.</param>
/// <param name="Control">The control itself.</param>
public sealed record Clickable(string Screen, string Label, string Kind, Control Control)
{
    /// <summary>The key in the record and in the exclusion list: stable across a restyle, because it is what the control says.</summary>
    public string Key => Screen + ": " + Label;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 117 section 3b: "every control, found by walking the window rather than by a list somebody maintains". This
/// collects everything a person can click on a screen, so both the interface benchmark and its coverage test work from what the window
/// actually has rather than from anything written down beside it.
/// </summary>
public static class ControlWalk
{
    /// <summary>Everything clickable and visible on the window as it stands.</summary>
    public static IReadOnlyList<Clickable> On(MainWindow window, string screen)
    {
        ArgumentNullException.ThrowIfNull(window);
        var found = new List<Clickable>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var control in window.GetLogicalDescendants().OfType<Control>())
        {
            if (!Showing(control) || !control.IsEffectivelyEnabled)
            {
                continue;
            }

            string? kind = Kind(control);
            if (kind is null)
            {
                continue;
            }

            string label = Label(control);
            var clickable = new Clickable(screen, label, kind, control);
            if (seen.Add(clickable.Key))
            {
                found.Add(clickable);
            }
        }

        return found;
    }

    /// <summary>
    /// Whether a person can see it: every screen's body is in the window all the time, hidden by its own IsVisible, and a control inside a
    /// hidden body is not on the screen whatever it says about itself.
    /// </summary>
    private static bool Showing(Control control)
    {
        for (ILogical? node = control; node is not null; node = node.LogicalParent)
        {
            if (node is Control { IsVisible: false })
            {
                return false;
            }
        }

        return true;
    }

    private static string? Kind(Control control) => control switch
    {
        // A repeat button is a scroll bar's arrow, which is the platform's and not GroupLab's.
        RepeatButton => null,
        ToggleButton => "toggle",
        Button => "button",
        MenuItem { Items.Count: 0 } => "menu item",
        TabItem => "tab",
        Expander => "disclosure",
        ComboBox => "dropdown",
        ListBox => "list",
        _ => null,
    };

    /// <summary>What the control says, which is how a person would name it, falling back to its own name and then to its type.</summary>
    private static string Label(Control control)
    {
        string? text = control switch
        {
            ContentControl { Content: string s } => s,
            ContentControl { Content: TextBlock { Text: { } t } } => t,
            ContentControl { Content: Panel panel } => panel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
            HeaderedContentControl { Header: string h } => h,
            _ => null,
        };

        // A control that says nothing is named by its tip first and its caption second: a tip is written for the person, while a caption is
        // whatever text happens to sit above it, which on the marking screen is the open file's name and moves with every image.
        text ??= (control as HeaderedContentControl)?.Header as string;
        text ??= control.Name;
        text ??= ToolTip.GetTip(control) as string;
        text ??= Caption(control);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = control.GetType().Name + " " + control.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return text.ReplaceLineEndings(" ").Trim();
    }

    /// <summary>
    /// The caption a dropdown or a nameless control sits under: the last piece of text before it among its parent's children, which is how a
    /// person would name it out loud. A control with no caption at all keeps its type and its place, which is still a name that does not move.
    /// </summary>
    private static string? Caption(Control control)
    {
        if (((ILogical)control).LogicalParent is not Panel parent)
        {
            return null;
        }

        string? caption = null;
        foreach (var child in parent.Children)
        {
            if (ReferenceEquals(child, control))
            {
                return caption;
            }

            string? text = child switch
            {
                TextBlock { Text: { } t } when !string.IsNullOrWhiteSpace(t) => t,
                ContentControl { Content: string s } => s,
                _ => null,
            };
            caption = text ?? caption;
        }

        return caption;
    }
}
