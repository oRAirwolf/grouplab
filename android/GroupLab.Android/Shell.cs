using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using Button = Avalonia.Controls.Button;
using RadioButton = Avalonia.Controls.RadioButton;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3: the application's frame. Three places along the bottom, where a thumb reaches them on a phone
/// and on the Fold 7 either way up: Capture, Sessions and Settings. The first run questions come before any of them, until each open one
/// is answered. Back from Sessions or Settings returns to Capture; Back from Capture leaves, as Android expects.
/// </summary>
public sealed class Shell : UserControl
{
    /// <summary>The places, in the order they sit along the bottom.</summary>
    internal enum Place
    {
        Capture,
        Sessions,
        Settings,
    }

    /// <summary>Whether the project takes targets from the application: the build's limits.json, as on the desktop.</summary>
    internal static bool TargetsOpen => ReceiverTerms.Current.AppOpen;

    /// <summary>Whether the project takes error reports: the build's limits.json, as on the desktop.</summary>
    internal static bool ErrorsOpen => ReceiverTerms.Current.ErrorReportsOpen;

    /// <summary>Whether the project takes hardware survey reports: the build's limits.json, as on the desktop.</summary>
    internal static bool SurveyOpen => ReceiverTerms.Current.SurveyOpen;

    private readonly ContentControl page = new();
    private readonly Dictionary<Place, Button> tabs = [];
    private readonly UniformGrid bar = new() { Rows = 1 };
    private readonly DockPanel frame = new();

    /// <summary>The capture page is kept, so going to Settings and back does not lose a result on screen.</summary>
    private CapturePage? capture;

    internal Place Showing { get; private set; } = Place.Capture;

    public Shell()
    {
        foreach (var place in Enum.GetValues<Place>())
        {
            var tab = new Button
            {
                Content = place.ToString(),
                MinHeight = 56,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(0),
            };
            tab.Click += (_, _) => Show(place);
            tabs[place] = tab;
            bar.Children.Add(tab);
        }

        DockPanel.SetDock(bar, Dock.Bottom);
        frame.Children.Add(bar);
        frame.Children.Add(page);
        AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is { } top)
            {
                top.BackRequested += (_, e) => e.Handled = Back();
            }
        };

        if (FirstRunView.Due(App.Settings))
        {
            Content = new FirstRunView(App.Settings, () =>
            {
                Content = frame;
                Show(Place.Capture);
            });
        }
        else
        {
            Content = frame;
            Show(Place.Capture);
        }
    }

    internal void Show(Place place)
    {
        Showing = place;
        foreach (var (each, tab) in tabs)
        {
            tab.FontWeight = each == place ? FontWeight.Bold : FontWeight.Normal;
        }

        DiagnosticLog.Info("ui.place", ("place", place.ToString()));
        page.Content = place switch
        {
            Place.Settings => new SettingsView(App.Settings),
            Place.Sessions => new SessionsPage(),
            _ => capture ??= new CapturePage(),
        };
    }

    /// <summary>Back returns to Capture from anywhere else, and is left to Android on Capture itself.</summary>
    internal bool Back()
    {
        if (Content != frame || Showing == Place.Capture)
        {
            return false;
        }

        Show(Place.Capture);
        return true;
    }
}

/// <summary>The small pieces every screen here is built from: a heading, a line that wraps, and a button a thumb can hit.</summary>
internal static class Screens
{
    public const double Touch = 48;

    public static TextBlock Heading(string text) => new() { Text = text, FontSize = 22, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

    public static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    public static Button Choice(string words, Action chosen)
    {
        var button = new Button
        {
            Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap },
            MinHeight = Touch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        button.Click += (_, _) => chosen();
        return button;
    }

    public static RadioButton Radio(string group, string words, bool chosen) =>
        new() { GroupName = group, Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap }, MinHeight = Touch, IsChecked = chosen };

    /// <summary>A page of words: a heading and a paragraph, scrolled when it does not fit.</summary>
    public static Control Words(string heading, string words) => Page(new StackPanel { Spacing = 12, Children = { Heading(heading), Line(words) } });

    /// <summary>Any page's column: a margin, no wider than reads well on the Fold 7 open or a tablet, and scrolled.</summary>
    public static Control Page(Control column)
    {
        column.Margin = new Thickness(16);
        column.MaxWidth = 640;
        column.HorizontalAlignment = HorizontalAlignment.Stretch;
        return new ScrollViewer { Content = column, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
