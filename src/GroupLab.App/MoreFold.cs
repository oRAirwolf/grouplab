using Avalonia.Controls;
using Avalonia.Layout;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 299: Alan found Settings' sharing sections "very wordy", taking up too much space. Each section now shows
/// its choice and one short line; the full explanation sits behind "More", closed at first and remembered once opened, so every word that
/// was there is still one tap away. The desktop and the phone draw the same fold; the Android and iOS applications compile this file as it is.
/// </summary>
internal static class MoreFold
{
    public const string Closed = "More ▸";

    public const string Open = "Less ▾";

    /// <summary>The settings key a fold's state is kept under.</summary>
    public static string Key(string item) => "settings.more." + item;

    /// <summary>
    /// The "More" button and the body it opens, beneath it. <paramref name="item"/> names the fold for the settings file;
    /// <paramref name="minHeight"/> gives a phone's finger its room.
    /// </summary>
    public static StackPanel Make(AppSettingsStore settings, string item, IEnumerable<Control> body, double minHeight = 0)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var inside = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var control in body)
        {
            inside.Children.Add(control);
        }

        bool open = settings.LoadWhyOpen(Key(item));
        var toggle = new Button { HorizontalAlignment = HorizontalAlignment.Left, MinHeight = minHeight, Classes = { AppStyles.Link } };
        void Show()
        {
            inside.IsVisible = open;
            toggle.Content = open ? Open : Closed;
            ToolTip.SetTip(toggle, open ? "Hide the full explanation" : "Show the full explanation");
        }

        toggle.Click += (_, _) =>
        {
            open = !open;
            settings.SaveWhyOpen(Key(item), open);
            Show();
        };
        Show();
        return new StackPanel { Spacing = Tokens.Space4, Children = { toggle, inside } };
    }
}
