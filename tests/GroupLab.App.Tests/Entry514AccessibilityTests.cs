using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App.Theme;
using GroupLab.Tests.Support;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 514 section 3, an accessibility pass that needs no person, ahead of request 105's screen reader sitting.
/// Entry 447 audits every screen and window in the theme GroupLab starts in; this holds the same screens and windows to the same audit
/// (<see cref="LayoutAudit"/>: nothing cut off, past the edge or drawn over, every line of words at WCAG AA contrast against what is behind
/// it) in the light theme and the high contrast one too, and to the screen reader's audit, since a theme can hide or reveal a control.
/// </summary>
public class Entry514AccessibilityTests
{
    private static readonly ThemeChoice[] Themes = [ThemeChoice.Light, ThemeChoice.HighContrast, ThemeChoice.Dark];

    [AvaloniaFact]
    public void EveryScreenInEveryThemeIsWholeAndReadable()
    {
        var all = new List<string>();
        var backgrounds = new HashSet<string>();
        foreach (var theme in Themes)
        {
            var (window, path) = AnalysisStateTests.Marked();
            try
            {
                window.CalibreAnswered();
                window.SetTheme(theme);
                Dispatcher.UIThread.RunJobs();
                backgrounds.Add(BackgroundOf(window));
                foreach (string place in new[] { "marking", "analysis", "Sessions", "Library", "Ballistics", "Compare", "Equipment" }.Concat(SettingsMenu.All.Select(c => "Settings, " + c)))
                {
                    if (place == "analysis")
                    {
                        window.Analyse();
                    }
                    else if (place.StartsWith("Settings, ", StringComparison.Ordinal))
                    {
                        window.ShowSettings(Enum.Parse<SettingsCategory>(place["Settings, ".Length..]));
                    }
                    else if (place != "marking")
                    {
                        window.Go(Enum.Parse<Destination>(place));
                    }

                    Dispatcher.UIThread.RunJobs();
                    Dispatcher.UIThread.RunJobs();
                    all.AddRange(LayoutAudit.Findings(window).Select(f => $"{theme}, {place}: {f}"));
                    all.AddRange(ScreenReaderAudit.Findings(window, null).Select(f => $"{theme}, {place}: {f}"));
                }
            }
            finally
            {
                window.Close();
                File.Delete(path);
            }
        }

        // The themes really changed what was audited: three themes, three backgrounds.
        Assert.Equal(Themes.Length, backgrounds.Count);
        Assert.True(all.Count == 0, all.Count + " findings:\n" + string.Join("\n", all.Distinct()));
    }

    /// <summary>The colours the words are drawn in, which every theme changes.</summary>
    private static string BackgroundOf(Window window) => string.Join(",",
        window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Foreground)
            .OfType<Avalonia.Media.ISolidColorBrush>().Select(b => b.Color.ToString()).Distinct().Order(StringComparer.Ordinal));

    [AvaloniaFact]
    public void ItsOwnWindowsInEveryThemeAreWholeAndReadable()
    {
        var all = new List<string>();
        foreach (var theme in Themes)
        {
            var (window, path) = AnalysisStateTests.Marked();
            try
            {
                window.SetTheme(theme);
                window.OpenPrinterCheck();
                window.ShowSettings(SettingsCategory.Help);
                Dispatcher.UIThread.RunJobs();
                window.GetVisualDescendants().OfType<Button>().First(b => b.Content is string words && words.StartsWith("Report a problem", StringComparison.Ordinal))
                    .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                foreach (var owned in window.OwnedWindows.ToList())
                {
                    all.AddRange(LayoutAudit.Findings(owned).Select(f => $"{theme}, {owned.Title}: {f}"));
                    owned.Close();
                }
            }
            finally
            {
                window.Close();
                File.Delete(path);
            }
        }

        Assert.True(all.Count == 0, all.Count + " findings:\n" + string.Join("\n", all.Distinct()));
    }
}
