using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 312 section 5, from Alan's iPad: changing a caliber meant deleting everything in the box before typing. The
/// caliber box on the desktop and on the phone selects all of its text when it is tapped or tabbed into, so typing replaces it at once, and
/// carries a clear button inside it that empties it. The Android and iOS applications compile this file as it is.
/// </summary>
internal static class CaliberBoxes
{
    /// <summary>
    /// Selects all of the box's text when it takes the focus. A tap or a click places the caret when the finger or the button comes up, so a
    /// focus by pointer selects on the release that follows it, and a focus by the keyboard selects at once.
    /// </summary>
    public static void SelectAllOnFocus(AutoCompleteBox box)
    {
        ArgumentNullException.ThrowIfNull(box);
        bool pressed = false;
        box.GotFocus += (_, e) =>
        {
            if (e.NavigationMethod == NavigationMethod.Pointer)
            {
                pressed = true;
                return;
            }

            Dispatcher.UIThread.Post(() => SelectAll(box));
        };
        box.AddHandler(InputElement.PointerReleasedEvent, (_, _) =>
        {
            if (pressed)
            {
                pressed = false;
                Dispatcher.UIThread.Post(() => SelectAll(box));
            }
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>All of the box's text, selected, where it has a text field inside it.</summary>
    public static void SelectAll(AutoCompleteBox box)
    {
        ArgumentNullException.ThrowIfNull(box);
        if (box.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() is { } field)
        {
            field.SelectAll();
        }
    }

    /// <summary>
    /// The clear button inside the box, at its right: it empties the box, closes the list and gives the box the focus, so the next thing
    /// typed is the new caliber. It shows only while there is something to clear.
    /// </summary>
    public static Button Clear(AutoCompleteBox box, double side, Action? cleared = null)
    {
        ArgumentNullException.ThrowIfNull(box);
        var button = new Button
        {
            Content = Icons.Draw(ClearMark, 12),
            MinWidth = side,
            MinHeight = side,
            Padding = new Thickness(0),
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = !string.IsNullOrEmpty(box.Text),
            Focusable = false,
        };
        ToolTip.SetTip(button, "Clear the caliber");
        Avalonia.Automation.AutomationProperties.SetName(button, "Clear the caliber");
        button.Click += (_, _) =>
        {
            box.Text = "";
            box.IsDropDownOpen = false;
            box.Focus();
            cleared?.Invoke();
        };
        box.TextChanged += (_, _) => button.IsVisible = !string.IsNullOrEmpty(box.Text);
        box.InnerRightContent = button;
        return button;
    }

    /// <summary>A cross, drawn in the same 16 unit square as the other icons.</summary>
    public const string ClearMark = "M3,2 L8,6.9 L13,2 L14,3 L9.1,8 L14,13 L13,14 L8,9.1 L3,14 L2,13 L6.9,8 L2,3 Z";
}
