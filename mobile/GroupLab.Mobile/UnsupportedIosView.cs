using Avalonia;
using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 505 section 1, Alan's words: GroupLab installs on iOS 18 to 25 but is supported only on iOS 26 and later,
/// so a device below 26 is told so once, before the first run's questions, and goes on only when the person has said they understand.
/// The acceptance is kept with the version it was given on: the warning comes back only while the device is below 26 and its version has
/// changed, and never on 26 or later. The way back to a floor of 26 is in docs/IOS-PLAN.md.
/// </summary>
public static class UnsupportedIos
{
    /// <summary>The lowest iOS version GroupLab is supported on.</summary>
    public const int Floor = 26;

    public const string Title = "iOS 26 is the minimum supported version";

    public const string Understand = "I understand there is no support and no guarantee on this version.";

    public const string TryAnyway = "Try GroupLab anyway";

    public const string NotNow = "Not now";

    public const string NotStartedTitle = "GroupLab has not started";

    public const string NotStarted = "Nothing was saved. You can close GroupLab now, or read the warning again when you are ready.";

    public const string ReadAgain = "Read the warning again";

    public static string Says(string version) =>
        $"This device has iOS {version}. GroupLab is built and tested for iOS 26 and later. You can still try it here, but if anything goes wrong you will not receive support, and there is no guarantee GroupLab will keep working on this version in the future. If your device can update, Settings, General, Software Update installs iOS 26 for free.";

    /// <summary>The one line in Settings, About, on such a device.</summary>
    public static string AboutLine(string version) => $"Unsupported iOS version ({version}): no support, no guarantee.";

    /// <summary>Whether <paramref name="version"/> is an iOS version below the floor; null or unreadable is not.</summary>
    public static bool Below(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string major = version.Trim().Split('.')[0];
        return int.TryParse(major, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int m) && m < Floor;
    }

    /// <summary>Whether the warning is shown at this start: below the floor, and not accepted on this same version.</summary>
    public static bool Due(string? version, string? acceptedOn) => Below(version) && !string.Equals(version!.Trim(), acceptedOn, StringComparison.Ordinal);
}

/// <summary>
/// The warning itself, in the look of the first run's questions: the page's title, the words, the switch, and the two ways on at the
/// bottom. "Try GroupLab anyway" stays off until the switch is on. iOS gives an application no way to close itself (Apple's guidelines
/// ask that it never does), so "Not now" stops the start where it is, saving nothing, and says so.
/// </summary>
public sealed class UnsupportedIosView : UserControl
{
    public UnsupportedIosView(string version, Action accepted)
    {
        version = version.Trim();
        DiagnosticLog.Info("unsupported-ios.shown", ("version", version));
        var understand = new CheckBox
        {
            Content = new TextBlock { Text = UnsupportedIos.Understand, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            MinHeight = Screens.Touch,
        }.Id("unsupported-ios-understand");
        Avalonia.Automation.AutomationProperties.SetName(understand, UnsupportedIos.Understand);
        var column = new StackPanel
        {
            Spacing = 24,
            Children =
            {
                Screens.Title(UnsupportedIos.Title),
                new StackPanel { Spacing = 8, Children = { Screens.Line(UnsupportedIos.Says(version)), understand } },
            },
        };

        var go = Screens.Primary(UnsupportedIos.TryAnyway, () =>
        {
            if (understand.IsChecked != true)
            {
                return;
            }

            DiagnosticLog.Info("unsupported-ios.accepted", ("version", version));
            accepted();
        }).Id("unsupported-ios-try");
        go.IsEnabled = false;
        understand.IsCheckedChanged += (_, _) => go.IsEnabled = understand.IsChecked == true;

        Control warning = null!;
        var notNow = Screens.Choice(UnsupportedIos.NotNow, () =>
        {
            // Nothing is saved: the switch is not remembered, and the warning is shown at the next start.
            DiagnosticLog.Info("unsupported-ios.not-now", ("version", version));
            understand.IsChecked = false;
            Content = Stopped(() => Content = warning);
        }).Id("unsupported-ios-not-now");

        var foot = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8, Margin = new Thickness(16, 8, 16, 12), Children = { notNow, go } };
        Grid.SetColumn(go, 1);
        var page = Screens.Page(column);
        warning = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { page, foot } };
        Grid.SetRow(foot, 1);
        Content = warning;
    }

    private static Control Stopped(Action again)
    {
        var column = new StackPanel
        {
            Spacing = 24,
            Children =
            {
                Screens.Title(UnsupportedIos.NotStartedTitle),
                new StackPanel { Spacing = 8, Children = { Screens.Line(UnsupportedIos.NotStarted), Screens.Choice(UnsupportedIos.ReadAgain, again).Id("unsupported-ios-again") } },
            },
        };
        return Screens.Page(column);
    }
}
