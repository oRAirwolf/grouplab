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
/// and on the Fold 7 either way up: Capture, Sessions, Targets (entry 243 section 3.4) and Settings. The first run questions come before any of them, until each open one
/// is answered. Back from Sessions or Settings returns to Capture; Back from Capture leaves, as Android expects.
/// </summary>
public sealed class Shell : UserControl
{
    /// <summary>The places, in the order they sit along the bottom.</summary>
    internal enum Place
    {
        Capture,
        Sessions,

        // Entry 259 screen 5: Ballistics is its own place, between Sessions and Targets.
        Ballistics,
        Targets,
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

    /// <summary>The one shell, for the capture screen to hide the bar along the bottom while the camera fills the screen (entry 260).</summary>
    internal static Shell? Current { get; private set; }

    private readonly Border nav;

    public Shell()
    {
        Current = this;
        foreach (var place in Enum.GetValues<Place>())
        {
            // Entry 246, look B: each place an icon over its name; the current one's icon sits in an amber pill.
            var tab = new Button
            {
                Content = new StackPanel
                {
                    Spacing = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new Border { Child = GroupLab.App.Theme.Icons.Draw(Icon(place), 20), Classes = { PhoneStyles.NavPill } },
                        new TextBlock { Text = place.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Classes = { PhoneStyles.NavLabel } },
                    },
                },
                MinHeight = 56,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Classes = { PhoneStyles.NavItem },
            };
            tab.Click += (_, _) => Show(place);
            tabs[place] = tab;
            bar.Children.Add(tab);
        }

        nav = new Border { Child = bar, Classes = { PhoneStyles.Nav } };
        DockPanel.SetDock(nav, Dock.Bottom);
        frame.Children.Add(nav);
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

    /// <summary>Entry 259 screen 5: Ballistics with a result's group carried in, for its hit chance.</summary>
    internal void ShowBallistics(GroupLab.Core.Marking.MarkingState state)
    {
        Show(Place.Ballistics);
        page.Content = new BallisticsPage(state);
    }

    /// <summary>Hides the bar along the bottom, or shows it again.</summary>
    internal void Immersive(bool on) => nav.IsVisible = !on;

    internal void Show(Place place)
    {
        Immersive(false);
        Showing = place;
        foreach (var (each, tab) in tabs)
        {
            tab.Classes.Set(PhoneStyles.On, each == place);
        }

        DiagnosticLog.Info("ui.place", ("place", place.ToString()));
        page.Content = place switch
        {
            Place.Settings => new SettingsView(App.Settings),
            Place.Sessions => new SessionsPage(),
            Place.Targets => new TargetsPage(),
            Place.Ballistics => new BallisticsPage(),
            _ => capture ??= new CapturePage(),
        };
    }

    /// <summary>The desktop's icon for each place: the aim for Capture, the records for Sessions, the printer for Targets, the gear for Settings.</summary>
    private static string Icon(Place place) => place switch
    {
        Place.Capture => GroupLab.App.Theme.Icons.Aim,
        Place.Sessions => GroupLab.App.Theme.Icons.Records,
        Place.Targets => GroupLab.App.Theme.Icons.Print,
        Place.Ballistics => GroupLab.App.Theme.Icons.Ballistics,
        _ => GroupLab.App.Theme.Icons.Settings,
    };

    /// <summary>Back returns to Capture from anywhere else, and is left to Android on Capture itself.</summary>
    internal bool Back()
    {
        // Entry 260: on the camera, Android's back closes it rather than leaving the application.
        if (Content == frame && Showing == Place.Capture && capture?.CloseCamera() == true)
        {
            return true;
        }

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

    /// <summary>A page's own name, at the top of it, as look B has it (entry 246): the desktop's lead size.</summary>
    public static TextBlock Title(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Title } };

    /// <summary>A section's heading, at the desktop's value size.</summary>
    public static TextBlock Heading(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Heading } };

    public static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    /// <summary>Words that explain rather than say: smaller and dim.</summary>
    public static TextBlock Dim(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } };

    public static Button Choice(string words, Action chosen) => Pill(words, chosen, primary: false);

    /// <summary>The one thing a screen is for, in amber, as the desktop's primary action is.</summary>
    public static Button Primary(string words, Action chosen) => Pill(words, chosen, primary: true);

    private static Button Pill(string words, Action chosen, bool primary)
    {
        var button = new Button
        {
            Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        if (primary)
        {
            button.Classes.Add(PhoneStyles.Primary);
        }

        button.Click += (_, _) => chosen();
        return button;
    }

    /// <summary>A row of a list on a card: what it is, a dim line beneath, and a chevron that says it opens.</summary>
    public static Button Row(string words, string? detail, Action chosen)
    {
        var text = new StackPanel { Spacing = 2, Children = { Line(words) } };
        if (detail is not null)
        {
            text.Children.Add(Dim(detail));
        }

        var chevron = new TextBlock { Text = "›", VerticalAlignment = VerticalAlignment.Center, Classes = { PhoneStyles.Heading } };
        Grid.SetColumn(chevron, 1);
        var button = new Button
        {
            Content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8, Children = { text, chevron } },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Classes = { PhoneStyles.Row },
        };
        button.Click += (_, _) => chosen();
        return button;
    }

    /// <summary>One choice among several, as a card of its own that turns amber when chosen.</summary>
    public static RadioButton Radio(string group, string words, bool chosen) =>
        new() { GroupName = group, Content = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap }, IsChecked = chosen, Classes = { PhoneStyles.Choice } };

    /// <summary>Related things on one panel.</summary>
    public static Border Card(params Control[] children)
    {
        var inside = new StackPanel { Spacing = 8 };
        foreach (var child in children)
        {
            inside.Children.Add(child);
        }

        return new Border { Child = inside, Classes = { PhoneStyles.Card } };
    }

    /// <summary>Figures as tiles, two to a row: a label, the value in the desktop's figure face, and what it is in; one may be the headline.</summary>
    public static Control Tiles(IEnumerable<(string Label, string Value, string Under, bool Headline)> figures)
    {
        var grid = new Avalonia.Controls.Primitives.UniformGrid { Columns = 2 };
        foreach (var (label, value, under, headline) in figures)
        {
            var tile = new Border
            {
                Margin = new Thickness(4),
                Child = new StackPanel
                {
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.TileLabel } },
                        new TextBlock { Text = value, Classes = { PhoneStyles.TileValue } },
                        new TextBlock { Text = under, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.TileLabel } },
                    },
                },
                Classes = { PhoneStyles.Tile },
            };
            if (headline)
            {
                tile.Classes.Add(PhoneStyles.TileHeadline);
            }

            grid.Children.Add(tile);
        }

        return grid;
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 243 section 3.2: a long analysis says what it is doing, a step at a time, and can be canceled. Returns the
    /// page, the line to update and the button, so the caller wires the cancel.
    /// </summary>
    public static (Control Page, TextBlock Line, Button Cancel) Progress(string heading)
    {
        var line = Line(GroupLab.Core.Trace.StageWords.Starting);
        var bar = new Avalonia.Controls.ProgressBar { IsIndeterminate = true, MinHeight = 6 };
        var cancel = new Button { Content = new TextBlock { Text = "Cancel" }, HorizontalAlignment = HorizontalAlignment.Left };
        return (Page(new StackPanel { Spacing = 12, Children = { Title(heading), Card(bar, line), cancel } }), line, cancel);
    }

    /// <summary>A page of words: a title and a paragraph, scrolled when it does not fit.</summary>
    public static Control Words(string heading, string words) => Page(new StackPanel { Spacing = 12, Children = { Title(heading), Line(words) } });

    /// <summary>Any page's column: a margin, no wider than reads well on the Fold 7 open or a tablet, and scrolled.</summary>
    public static Control Page(Control column)
    {
        column.Margin = new Thickness(16);
        column.MaxWidth = 640;
        column.HorizontalAlignment = HorizontalAlignment.Stretch;
        return new ScrollViewer { Content = column, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
