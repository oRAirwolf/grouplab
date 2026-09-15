using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 42: the palettes meet 4.5:1 for every text colour on every surface text sits on, no colour literal escapes
/// <c>Theme/Tokens.cs</c>, and the theme choice, dark, light or following the system, is applied and remembered.
/// </summary>
public partial class ThemeTests
{
    private static string Repository([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static double Luminance(Color c)
    {
        static double Channel(byte v) => v / 255.0 <= 0.03928 ? v / 255.0 / 12.92 : Math.Pow(((v / 255.0) + 0.055) / 1.055, 2.4);
        return (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
    }

    private static double Contrast(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [Fact]
    public void EveryTextColourReachesFourAndAHalfToOneOnEverySurfaceInBothThemes()
    {
        var failures = new List<string>();
        foreach (var (name, palette) in new[] { ("dark", Tokens.Dark), ("light", Tokens.Light) })
        {
            foreach (var (role, colour) in palette.TextColours)
            {
                foreach (var (surface, background) in palette.TextSurfaces)
                {
                    if (Contrast(colour, background) < 4.5)
                    {
                        failures.Add($"{name} {role} on {surface}: {Contrast(colour, background):0.00}");
                    }
                }
            }

            foreach (var (pair, fore, back) in new[] { ("primary text on amber", palette.OnAmber, palette.Amber), ("amber on its tint", palette.Amber, palette.AmberTint), ("teal on its tint", palette.Teal, palette.TealTint) })
            {
                if (Contrast(fore, back) < 4.5)
                {
                    failures.Add($"{name} {pair}: {Contrast(fore, back):0.00}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>Entry 42 section 6: a crude test that saves the light theme from dying by a thousand hard coded greys.</summary>
    [Fact]
    public void NoColourLiteralAppearsOutsideTheTokens()
    {
        string app = Path.Combine(Repository(), "src", "GroupLab.App");
        string tokens = Path.Combine(app, "Theme", "Tokens.cs");
        var failures = new List<string>();
        foreach (string file in Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories))
        {
            if (file == tokens || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (ColourLiteral().IsMatch(lines[i]))
                {
                    failures.Add($"{Path.GetRelativePath(app, file)} line {i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(failures.Count == 0, "colour literals outside Theme/Tokens.cs; name the colour in Tokens and use it from there:\n" + string.Join("\n", failures));
    }

    [AvaloniaFact]
    public void TheThemeChoiceIsAppliedToTheWindowAndRemembered()
    {
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppSettingsStore(settings);
            store.SaveUnits(UnitSettings.Imperial);
            store.SaveTheme(ThemeChoice.Light);
            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Light, window.ActualThemeVariant);
            Assert.Equal(Tokens.Light.Bg, Assert.IsAssignableFrom<ISolidColorBrush>(window.Background).Color);

            window.SetTheme(ThemeChoice.Dark);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Assert.Equal(Tokens.Dark.Bg, Assert.IsAssignableFrom<ISolidColorBrush>(window.Background).Color);

            var reopened = new AppSettingsStore(settings);
            Assert.Equal(ThemeChoice.Dark, reopened.LoadTheme());
            Assert.Equal(UnitSettings.Imperial, reopened.LoadUnits());

            window.SetTheme(ThemeChoice.System);
            Assert.Equal(ThemeVariant.Default, Application.Current!.RequestedThemeVariant);
            window.Close();
        }
        finally
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
            File.Delete(settings);
        }
    }

    [GeneratedRegex(@"Brushes\.|Colors\.|Color\.(From|Parse)|new Color\(|""#[0-9A-Fa-f]{3,8}""")]
    private static partial Regex ColourLiteral();
}
