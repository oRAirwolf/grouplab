using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker A item 2, the headless layer: the phone's counterpart of the desktop's sweep. Every place along
/// the bottom, a result as the phone shows one, and every page or sheet one press away from them, at the smallest phone (320 by 568), at an
/// iPad (1024 by 1366) and turned on their sides (568 by 320, 1366 by 1024), in both themes and with large text. On each:
/// <list type="bullet">
/// <item>every control a person acts on has a name a screen reader announces;</item>
/// <item>it lies inside the window's width, and below the window's bottom only where something scrolls it into view;</item>
/// <item>no line of text is cut off: none runs past the window's edge, none longer than its box on one line, none taller than its box;</item>
/// <item>each field stays above the keyboard with its confirming button (entry 328's rule, on the two upright sizes);</item>
/// <item>pressing each button that changes nothing for good neither throws nor leaves a page with no way back: the places along the bottom
/// showing, or a Back, Cancel, Close or Done on the page.</item>
/// </list>
/// <para>
/// <b>What it cannot check.</b> The headless platform has no real keyboard, camera, photo picker, share sheet or system dialog: the test phone
/// records them, and the camera's own screen is the platform's, so the buttons that open it are not pressed (<see cref="Platforms"/>). The app
/// does not follow the system's text size (neither head reads it), so "large text" here is a base size raised to 22 points; text a style sets
/// to its own size does not grow with it, as it does not on a phone. It does not press what a press opens, only one press deep, and it never
/// presses what deletes or sends (<see cref="Lasting"/>). Rotation is the landscape size laid out afresh, not a turn mid-page. Safe areas,
/// notches and the iOS home indicator are the heads' own and are not here; the simulator layer (the screen sweep in ios-app.yml) sees them.
/// </para>
/// </summary>
public partial class MobileSweepTests
{
    /// <summary>The smallest phone, an iPad, and both turned on their sides.</summary>
    public static TheoryData<int, int> Sizes => new() { { 320, 568 }, { 1024, 1366 }, { 568, 320 }, { 1366, 1024 } };

    /// <summary>The base text size the large text runs use, the size entry 323's and 348's narrow tests use.</summary>
    internal const double LargeText = 22;

    /// <summary>Buttons whose press changes something for good or reaches outside the phone, never pressed by the sweep.</summary>
    [GeneratedRegex(@"\b(Delete|Remove|Forget|Clear|Erase|Reset|Start again|Send|Upload|Submit|Install|Update now|Restart|Report a problem|Leave unpaired|Keep this pairing|Keep this distance|Keep|Save|Accept)\b", RegexOptions.IgnoreCase)]
    internal static partial Regex Lasting();

    /// <summary>Buttons that open the platform's own screens, which the test phone stands in for and the sweep cannot see: the camera.</summary>
    [GeneratedRegex(@"\b(Take a picture|Take a photo|Take the picture|Camera)\b", RegexOptions.IgnoreCase)]
    internal static partial Regex Platforms();

    /// <summary>The words of a way back, on a page that hides the places along the bottom.</summary>
    [GeneratedRegex(@"^(‹|Back|Cancel|Close|Done|Not now|Skip|Later|No\b)", RegexOptions.IgnoreCase)]
    internal static partial Regex WayBack();

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static bool Actionable(Control c) => c is Button or ToggleButton or ComboBox or TextBox or Slider or ListBox;

    private static bool Inner(Control c) => c.GetVisualAncestors().OfType<Control>().Any(a => a is ComboBox or ListBox or TextBox or ScrollBar or Slider);

    private static string NameOf(Control c) => ControlAutomationPeer.CreatePeerForElement(c).GetName() ?? "";

    private static string Describe(Control c)
    {
        string id = Avalonia.Automation.AutomationProperties.GetAutomationId(c) ?? "";
        string words = NameOf(c);
        return $"{c.GetType().Name} '{(words.Length > 0 ? words[..Math.Min(50, words.Length)] : id)}'";
    }

    private static PhoneResult? result;

    /// <summary>The committed sample scan read once, as Capture reads a picture, for the result's page.</summary>
    private static PhoneResult Result()
    {
        if (result is null)
        {
            string copy = Path.Combine(Phone.Platform.CacheFolder, "chosen-sweep.png");
            File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), copy, overwrite: true);
            result = PhoneAnalysis.Run(copy, new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, null, CancellationToken.None);
        }

        return result;
    }

    /// <summary>The places along the bottom, and a result.</summary>
    private static IEnumerable<(string Name, Action<Shell> Show)> Places() =>
    [
        .. Enum.GetValues<Shell.Place>().Select(p => (p.ToString(), (Action<Shell>)(s => s.Show(p)))),
        ("a result", s =>
        {
            s.Show(Shell.Place.Capture);
            s.ShowInPage(new ResultView(Result(), new ShotSetup(Calibre.Of(0.308), 3600), UnitSettings.Imperial, () => s.Show(Shell.Place.Capture)));
        }),
    ];

    private static (Shell Shell, Window Window) Started(int width, int height, bool large)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = width, Height = height, Content = shell };
        if (large)
        {
            window.FontSize = LargeText;
        }

        window.Show();
        Settle(window);
        return (shell, window);
    }

    /// <summary>Everything wrong with what the window shows now.</summary>
    internal static List<string> Problems(Window window, string where)
    {
        Settle(window);
        var found = new List<string>();
        double w = window.Bounds.Width, h = window.Bounds.Height;
        bool ScrollsDown(Control c) => c.GetVisualAncestors().OfType<ScrollViewer>().Any(s => s.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled);
        bool ScrollsAcross(Control c) => c.GetVisualAncestors().OfType<ScrollViewer>().Any(s => s.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled);
        var shown = window.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && c.Bounds.Width > 0 && c.Bounds.Height > 0).ToList();
        foreach (var c in shown.Where(c => Actionable(c) && c.IsEffectivelyEnabled && !Inner(c)))
        {
            // A content with no words of its own is announced by its type's name, "Avalonia.Controls.Grid", which is no name either.
            if (string.IsNullOrWhiteSpace(NameOf(c)) || NameOf(c).StartsWith("Avalonia.", StringComparison.Ordinal))
            {
                found.Add($"{where}: {Describe(c)} has no name a screen reader can announce");
            }

            if (c.TranslatePoint(default, window) is not { } at)
            {
                continue;
            }

            if ((at.X < -0.5 || at.X + c.Bounds.Width > w + 0.5) && !ScrollsAcross(c))
            {
                found.Add($"{where}: {Describe(c)} runs from {at.X:0} to {at.X + c.Bounds.Width:0}, outside the window's {w:0}");
            }

            if (at.Y + c.Bounds.Height > h + 0.5 && !ScrollsDown(c))
            {
                found.Add($"{where}: {Describe(c)} is below the window's bottom and nothing scrolls it into view");
            }
        }

        foreach (var t in shown.OfType<TextBlock>().Where(t => !string.IsNullOrEmpty(t.Text) && !Inner(t)))
        {
            string words = t.Text!.Length > 40 ? t.Text[..40] + "..." : t.Text;
            if (t.TranslatePoint(default, window) is { } at && at.X + t.Bounds.Width > w + 0.5 && !ScrollsAcross(t))
            {
                found.Add($"{where}: '{words}' runs past the window's right edge");
                continue;
            }

            var layout = t.TextLayout;
            if (t.TextWrapping == Avalonia.Media.TextWrapping.NoWrap && layout.WidthIncludingTrailingWhitespace > t.Bounds.Width + 1.5 && !ScrollsAcross(t))
            {
                found.Add($"{where}: '{words}' is one line {layout.WidthIncludingTrailingWhitespace:0} wide in a box {t.Bounds.Width:0} wide, so it is cut off");
            }
            else if (layout.Height > t.Bounds.Height + 1.5)
            {
                found.Add($"{where}: '{words}' is {layout.Height:0} tall in a box {t.Bounds.Height:0} tall, so its last lines are cut off");
            }
        }

        return found;
    }

    /// <summary>Every place and a result, at a size, in both themes, at the usual and the large text size.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(Sizes))]
    public void EveryPlaceIsNamedInsideTheWindowAndUncut(int width, int height)
    {
        var found = new List<string>();
        var app = Avalonia.Application.Current!;
        var was = app.RequestedThemeVariant;
        try
        {
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                app.RequestedThemeVariant = theme;
                foreach (bool large in new[] { false, true })
                {
                    var (shell, window) = Started(width, height, large);
                    try
                    {
                        foreach (var (name, show) in Places())
                        {
                            show(shell);
                            found.AddRange(Problems(window, $"{name}, {theme}{(large ? ", large text" : "")}"));
                        }
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            }
        }
        finally
        {
            app.RequestedThemeVariant = was;
        }

        Assert.True(found.Count == 0, $"at {width} by {height}:{Environment.NewLine}" + string.Join(Environment.NewLine, found.Distinct().Take(80)));
    }

    /// <summary>
    /// Every button on every place and a result pressed in turn, on a fresh page each time: nothing throws, the page it leads to passes the
    /// same checks, and it has a way back. At the smallest phone in the light theme, and at the iPad on its side, dark, with large text.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(320, 568, false, false)]
    [InlineData(1366, 1024, true, true)]
    public void EveryButtonOnePressAwayWorksAndComesBack(int width, int height, bool dark, bool large)
    {
        var found = new List<string>();
        var app = Avalonia.Application.Current!;
        var was = app.RequestedThemeVariant;
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var (shell, window) = Started(width, height, large);
        int pressed = 0;
        try
        {
            foreach (var (place, show) in Places())
            {
                show(shell);
                Settle(window);
                var keys = Buttons(window).Select(b => Key(b)).ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    int nth = keys.Take(i).Count(k => k == key);
                    show(shell);
                    Settle(window);
                    var button = Buttons(window).Where(b => Key(b) == key).ElementAtOrDefault(nth);
                    string words = button is null ? "" : NameOf(button) + (Avalonia.Automation.AutomationProperties.GetAutomationId(button) is { Length: > 0 } id ? $" ({id})" : "");
                    if (button is null || Lasting().IsMatch(words) || Platforms().IsMatch(words) || button.Classes.Contains(PhoneStyles.NavItem))
                    {
                        continue;
                    }

                    string where = $"{place}, after '{words}'";
                    try
                    {
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Settle(window);
                        pressed++;
                    }
                    catch (Exception e)
                    {
                        found.Add($"{where}: the press threw {e.GetType().Name}: {e.Message}");
                        continue;
                    }

                    found.AddRange(Problems(window, where));
                    bool back = shell.NavShowing || Buttons(window).Any(b => b.IsEffectivelyEnabled && WayBack().IsMatch(NameOf(b).Trim()));
                    if (!back)
                    {
                        found.Add($"{where}: the page has no way back, neither the places along the bottom nor a Back, Cancel, Close or Done");
                    }

                    shell.Keyboard.CloseKeyboard();
                }
            }
        }
        finally
        {
            window.Close();
            app.RequestedThemeVariant = was;
        }

        Assert.True(pressed > 40, $"only {pressed} buttons were pressed");
        Assert.True(found.Count == 0, $"at {width} by {height}:{Environment.NewLine}" + string.Join(Environment.NewLine, found.Distinct().Take(80)));
    }

    private static List<Button> Buttons(Window window) =>
        [.. window.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible && b.IsEffectivelyEnabled && !Inner(b))];

    private static string Key(Button b) => Avalonia.Automation.AutomationProperties.GetAutomationId(b) is { Length: > 0 } id ? id : NameOf(b);

    /// <summary>Every field on every place and a result, and on each page one press away, stays above the keyboard (entry 328) at the two upright sizes.</summary>
    [AvaloniaTheory]
    [InlineData(320, 568, 253)]
    [InlineData(1024, 1366, 398)]
    public void EveryFieldStaysAboveTheKeyboard(int width, int height, int keyboard)
    {
        var (shell, window) = Started(width, height, large: false);
        int fields = 0;
        try
        {
            foreach (var (place, show) in Places())
            {
                show(shell);
                Settle(window);
                var boxes = window.GetVisualDescendants().OfType<TextBox>().Where(t => t.IsEffectivelyVisible && t.IsEffectivelyEnabled).ToList();
                foreach (var box in boxes)
                {
                    // A field focused the moment its page is first laid out is refused the focus headlessly; a person's tap comes later.
                    for (int tries = 0; tries < 3 && !ReferenceEquals(window.FocusManager?.GetFocusedElement(), box); tries++)
                    {
                        window.UpdateLayout();
                        box.Focus();
                        Settle(window);
                    }

                    Assert.Same(box, window.FocusManager?.GetFocusedElement());
                    Entry328Tests.ClearOfTheKeyboard(shell, window, box, keyboard, $"{place} at {width} by {height}, field {boxes.IndexOf(box) + 1} of {boxes.Count}, {Describe(box)}");
                    shell.Keyboard.CloseKeyboard();
                    Settle(window);
                    fields++;
                }
            }
        }
        finally
        {
            window.Close();
        }

        Assert.True(fields >= 5, $"only {fields} fields were found");
    }
}
