using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Button = Avalonia.Controls.Button;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using RadioButton = Avalonia.Controls.RadioButton;

namespace GroupLab.Mobile;

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

    /// <summary>Entry 273: the note that says what a tap on a number switched, at the bottom, for a few seconds.</summary>
    private readonly TextBlock toastWords = new() { TextWrapping = TextWrapping.Wrap };

    private readonly Border toast;

    /// <summary>A note that stays until it is closed, along the top: an update downloaded, or the one just installed (entry 288).</summary>
    private readonly Border notice = new() { IsVisible = false, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16, 12, 16, 0), Classes = { PhoneStyles.Card } };

    /// <summary>Entry 273: the units changed by a tap on a number; every page showing figures shows them again.</summary>
    internal static event Action? UnitsChanged;

    /// <summary>Entry 294: Settings changed a unit, so every open screen shows its figures again.</summary>
    internal static void Units() => UnitsChanged?.Invoke();

    /// <summary>The capture page is kept, so going to Settings and back does not lose a result on screen.</summary>
    private CapturePage? capture;

    internal Place Showing { get; private set; } = Place.Capture;

    /// <summary>The one shell, for the capture screen to hide the bar along the bottom while the camera fills the screen (entry 260).</summary>
    internal static Shell? Current { get; private set; }

    private readonly Border nav;

    /// <summary>
    /// Entry 290 section 6: on iOS the bar along the bottom reaches the screen's bottom edge, under the home indicator, as every iPhone
    /// application's does; the page's own color was left in a strip beneath it. The iOS head sets this before the Shell is made. Android
    /// keeps its bar above the gesture strip, as it was.
    /// </summary>
    internal static bool BarToBottomEdge { get; set; }

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
                        new TextBlock { Text = place.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Classes = { PhoneStyles.NavLabel } }, // one line on purpose: a tab's one-word name
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
        toast = new Border
        {
            Child = toastWords,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 20),
            Classes = { PhoneStyles.Card },
        };
        frame.Children.Add(new Grid { Children = { page, toast, notice } });

        // Entry 273: tap a number to switch units, the same setting everywhere, remembered.
        UnitTap.Current = () => Phone.Settings.LoadUnits();
        // Entry 280 section 1: a tap switches the number tapped and is remembered for its figure.
        UnitTap.Remembered = key => Phone.Settings.LoadFigureUnit(key);
        UnitTap.Tapped = (key, symbol, kind) =>
        {
            if (key is not null)
            {
                Phone.Settings.SaveFigureUnit(key, symbol);
            }

            Phone.Settings.SaveUnitTapped();
            DiagnosticLog.Info("units.tap", ("kind", kind.ToString()), ("to", symbol), ("remembered", key is not null));
            Toast((key is null ? "This number" : key[..key.LastIndexOf('|')]) + " now in " + symbol + (key is null ? "" : " · remembered"));
        };
        UnitTap.Apply = (units, kind) =>
        {
            Phone.Settings.SaveUnits(units);
            Phone.Settings.SaveUnitTapped();
            DiagnosticLog.Info("units.tap", ("kind", kind.ToString()), ("linear", units.Linear.ToString()), ("angular", units.Angular.ToString()), ("distance", units.Distance.ToString()));
            UnitsChanged?.Invoke();
            Toast(GroupLab.Core.Marking.UnitSwitch.Said(units, kind) + " · remembered");
        };
        AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is { } top)
            {
                top.BackRequested += (_, e) => e.Handled = Back();
            }
        };

        if (FirstRunView.Due(Phone.Settings))
        {
            Content = new FirstRunView(Phone.Settings, () =>
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
    internal void ShowBallistics(GroupLab.Core.Marking.MarkingState state, bool zeroOffset = false)
    {
        Show(Place.Ballistics);
        page.Content = new BallisticsPage(state, zeroOffset);
    }

    /// <summary>Entry 273: the printer check, under Settings, and back to Settings when it is done or skipped.</summary>
    internal void ShowPrinterCheck(string? name = null)
    {
        Show(Place.Settings);
        page.Content = new PrinterCheckPage(() => Show(Place.Settings), string.IsNullOrEmpty(name) ? (name is null ? null : PrinterProfileNewName()) : name);
    }

    /// <summary>A name for another printer: "My printer" where none has it, else "Printer 2" and on.</summary>
    private static string PrinterProfileNewName()
    {
        var taken = Phone.Settings.LoadPrinters().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        return taken.Contains(GroupLab.Core.Marking.PrinterProfile.DefaultName)
            ? Enumerable.Range(2, 99).Select(n => $"Printer {n}").First(n => !taken.Contains(n))
            : GroupLab.Core.Marking.PrinterProfile.DefaultName;
    }

    /// <summary>The note at the bottom, for three seconds.</summary>
    internal void Toast(string words)
    {
        toastWords.Text = words;
        toast.IsVisible = true;
        Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (toastWords.Text == words)
            {
                toast.IsVisible = false;
            }
        }, TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// A note along the top that stays until it is closed or acted on, entry 288: "Updated to nightly N" with a way to its notes, or an
    /// update downloaded and waiting. One at a time; a new one replaces the last.
    /// </summary>
    internal void Notice(string words, string? action = null, Action? acted = null)
    {
        var column = new StackPanel { Spacing = 8, Children = { Screens.Line(words) } };
        var buttons = new WrapPanel();
        if (action is not null && acted is not null)
        {
            buttons.Children.Add(Screens.Primary(action, () =>
            {
                notice.IsVisible = false;
                acted();
            }));
        }

        buttons.Children.Add(Screens.Choice("Close", () => notice.IsVisible = false));
        column.Children.Add(buttons);
        notice.Child = column;
        notice.IsVisible = true;
    }

    /// <summary>
    /// The black idle screen over everything (entry 268), for a head with no native one: iOS (entry 290 section 2 item 6). Close gives the
    /// screen back as it was.
    /// </summary>
    internal void ShowIdle()
    {
        var before = Content;

        // The window behind the safe area too (the status bar's and the home indicator's strips on iOS), black while it shows. The Shell
        // itself carries the safe area as its padding and paints it with its own background, which the iOS self-test found left light
        // there (entry 290 section 2 item 6), so the Shell's background is made black as well as the window's.
        var top = TopLevel.GetTopLevel(this);
        var behind = top?.Background;
        var own = Background;
        bool ownSet = IsSet(BackgroundProperty);
        if (top is not null)
        {
            top.Background = Avalonia.Media.Brushes.Black;
        }

        Background = Avalonia.Media.Brushes.Black;

        Content = new IdleScreen(() =>
        {
            Content = before;
            if (ownSet)
            {
                Background = own;
            }
            else
            {
                ClearValue(BackgroundProperty);
            }

            if (top is not null)
            {
                top.Background = behind;
            }
        });
        DiagnosticLog.Info("app.idle", ("shown", true));
    }

    /// <summary>
    /// The system gives the Shell the safe area as its padding. Where <see cref="BarToBottomEdge"/> is set, the bar reaches down through the
    /// bottom of that padding, its buttons kept where they were and its own surface filling the strip behind the home indicator.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (BarToBottomEdge && change.Property == PaddingProperty && nav is not null)
        {
            double below = Padding.Bottom;
            nav.Margin = new Thickness(0, 0, 0, -below);
            nav.Padding = new Thickness(0, GroupLab.App.Theme.Tokens.Space4, 0, GroupLab.App.Theme.Tokens.Space8 + below);
        }
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
            Place.Settings => new SettingsView(Phone.Settings),
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

    /// <summary>
    /// A page that must answer Back itself, entry 291 section 2.2: Fix holes asks whether to keep the changes rather than lose them. True
    /// where it did.
    /// </summary>
    internal static Func<bool>? BackOverride { get; set; }

    /// <summary>Back returns to Capture from anywhere else, and is left to Android on Capture itself.</summary>
    internal bool Back()
    {
        if (BackOverride is { } page && page())
        {
            return true;
        }

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
    // Entry 258: a secondary line that names a glossary word explains it when tapped, as on the desktop.
    public static TextBlock Dim(string text) => PhoneTerms.Explain(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } });

    /// <summary>
    /// A dim line of names, a session's sheet and date, that is never explained: entry 295 found "Load" in "GroupLab 5x5 Load Development
    /// with Load Block, Letter" underlining a whole line of names as though it explained a word.
    /// </summary>
    public static TextBlock Quiet(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } };

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
    public static Button Row(string words, string? detail, Action chosen, bool explain = true)
    {
        var text = new StackPanel { Spacing = 2, Children = { Line(words) } };
        if (detail is not null)
        {
            text.Children.Add(explain ? Dim(detail) : Quiet(detail));
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
                        // One line for a figure's number; a figure withheld in words ("not quoted below 5 shots") wraps rather than being cut off.
                        UnitTap.Attach(new TextBlock { Text = value, TextWrapping = value.Any(char.IsDigit) && value.Count(char.IsLetter) <= 4 ? TextWrapping.NoWrap : TextWrapping.Wrap, Classes = { PhoneStyles.TileValue } }, label),
                        UnitTap.Attach(new TextBlock { Text = under, TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.TileLabel } }, label),
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
