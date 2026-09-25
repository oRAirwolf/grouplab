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

    /// <summary>
    /// Entry 42 section 2 for dark and light, and NOTES-FROM-PLANNING.md entry 93 section 3 for high contrast, which is held to WCAG's AAA
    /// ratio rather than AA. All three palettes come from one token set, and this is the test that says so: the high contrast one is derived
    /// from the dark one in code, so a role whose meaning does not survive the derivation fails here rather than in somebody's eyes.
    /// </summary>
    [Fact]
    public void EveryTextColourReachesItsRatioOnEverySurfaceInEveryTheme()
    {
        var failures = new List<string>();
        foreach (var (name, palette, ratio) in new[] { ("dark", Tokens.Dark, 4.5), ("light", Tokens.Light, 4.5), ("high contrast", Tokens.HighContrast, Tokens.HighContrastRatio) })
        {
            foreach (var (role, colour) in palette.TextColours)
            {
                foreach (var (surface, background) in palette.TextSurfaces)
                {
                    if (Contrast(colour, background) < ratio)
                    {
                        failures.Add($"{name} {role} on {surface}: {Contrast(colour, background):0.00}, wanted {ratio:0.0}");
                    }
                }
            }

            foreach (var (pair, fore, back) in new[] { ("primary text on amber", palette.OnAmber, palette.Amber), ("amber on its tint", palette.Amber, palette.AmberTint), ("teal on its tint", palette.Teal, palette.TealTint) })
            {
                if (Contrast(fore, back) < ratio)
                {
                    failures.Add($"{name} {pair}: {Contrast(fore, back):0.00}, wanted {ratio:0.0}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 100 section 1: the form roles hold in every palette. A field's border and the focus ring reach WCAG's 3:1
    /// for user-interface edges against the field and the panel it sits on, 4.5:1 in high contrast; a disabled control's text reaches the
    /// same floor; and warning and error text are held to the text ratios by the test above, on fields as well as panels. Warning is amber
    /// and error is red in every theme, the meanings entry 93 fixed.
    /// </summary>
    [Fact]
    public void EveryFormRoleHoldsItsRatioAndItsMeaningInEveryTheme()
    {
        var failures = new List<string>();
        foreach (var (name, palette, ratio) in new[] { ("dark", Tokens.Dark, Tokens.EdgeRatio), ("light", Tokens.Light, Tokens.EdgeRatio), ("high contrast", Tokens.HighContrast, Tokens.HighContrastEdgeRatio) })
        {
            foreach (var (role, colour) in palette.FormEdges.Append(("disabled", palette.Disabled)))
            {
                foreach (var (surface, background) in new[] { ("field", palette.FieldBg), ("panel", palette.Panel) })
                {
                    if (Contrast(colour, background) < ratio)
                    {
                        failures.Add($"{name} {role} on {surface}: {Contrast(colour, background):0.00}, wanted {ratio:0.0}");
                    }
                }
            }

            Assert.True(palette.WarningText.R > palette.WarningText.B && palette.WarningText.G > palette.WarningText.B, $"{name}: warning text is not amber");
            Assert.True(palette.ErrorText.R > palette.ErrorText.G && palette.ErrorText.R > palette.ErrorText.B, $"{name}: error text is not red");
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Entry 93 section 3's guard as a test: the light and high contrast themes are the same roles as the dark one rather than separate
    /// inventions, and the accents keep their own hues, so "teal is what the software found and amber is what needs a person" survives a
    /// theme change. If a token set cannot produce the other themes, it is not a token set, it is a dark theme with names on it.
    /// </summary>
    [Fact]
    public void TheOtherThemesAreTheSameRolesAndKeepTheAccentsTheirOwnHues()
    {
        foreach (var (name, palette) in new[] { ("light", Tokens.Light), ("high contrast", Tokens.HighContrast) })
        {
            Assert.True(Contrast(palette.Text, palette.Bg) >= 4.5, $"{name}: text on the window");
            Assert.True(palette.Teal.G > palette.Teal.R, $"{name}: teal is no longer green");
            Assert.True(palette.Amber.R > palette.Amber.B, $"{name}: amber is no longer warm");
            Assert.True(palette.Alert.R > palette.Alert.G, $"{name}: alert is no longer red");
            Assert.NotEqual(Tokens.Dark.Bg, palette.Bg);
            Assert.NotEqual(Tokens.Dark.Text, palette.Text);
        }
    }
    /// <summary>
    /// Question 20, answered by NOTES-FROM-PLANNING.md entry 106 section 2: the mark's colours are brand roles, and the committed files and the
    /// roles are held to each other. Every colour in a mark file is one of its palette's three roles, and every role is in the lockup.
    /// </summary>
    [Fact]
    public void TheMarkFilesAreDrawnInThePalettesBrandRoles()
    {
        string assets = Path.Combine(Repository(), "src", "GroupLab.App", "Assets");
        foreach (var (file, palette) in new[] { ("grouplab-mark.svg", Tokens.Dark), ("grouplab-lockup.svg", Tokens.Dark), ("grouplab-mark-light.svg", Tokens.Light), ("grouplab-lockup-light.svg", Tokens.Light) })
        {
            var used = System.Xml.Linq.XDocument.Load(Path.Combine(assets, file)).Root!.Elements()
                .SelectMany(e => new[] { (string?)e.Attribute("fill"), (string?)e.Attribute("stroke") })
                .Where(c => c is not null && c != "none")
                .Select(c => Tokens.Ink(c!))
                .ToHashSet();
            var roles = palette.MarkColours.Select(r => r.Colour).ToHashSet();
            Assert.True(used.IsSubsetOf(roles), $"{file} uses a colour that is not one of its palette's mark roles");
            if (file.Contains("lockup", StringComparison.Ordinal))
            {
                Assert.True(roles.SetEquals(used), $"{file} does not use every mark role");
            }
        }
    }

    /// <summary>
    /// Entry 106 section 2: the mark is a graphic drawn at header size and larger, so its roles are held to 3:1 against the surfaces behind it,
    /// the panel the header and rail are drawn on and the window's background, in every theme. The ratio is computed here, not taken on trust.
    /// </summary>
    [Fact]
    public void TheMarkRolesClearThreeToOneOnTheSurfacesBehindThem()
    {
        var failures = new List<string>();
        foreach (var (name, palette) in new[] { ("dark", Tokens.Dark), ("light", Tokens.Light), ("high contrast", Tokens.HighContrast) })
        {
            foreach (var (role, colour) in palette.MarkColours)
            {
                foreach (var (surface, background) in new[] { ("panel", palette.Panel), ("bg", palette.Bg) })
                {
                    if (Contrast(colour, background) < 3)
                    {
                        failures.Add($"{name} {role} on {surface}: {Contrast(colour, background):0.00}");
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 169 section 3: the composite plot is high contrast in both themes. Every mark is in the ink, black on white
    /// or white on black, at AAA's 7:1; the ring grey and the one accent are held to 4.5:1, the ratio for text, although they are strokes; and
    /// the plot draws nothing faded or translucent, which is what made its rings pastel. Entry 204 changes two things on Alan's word: the
    /// shot outlines are at half strength, through <c>OutlineOpacity</c> and nothing else, and the bull's rings are a pale grey on purpose,
    /// so the ring grey below is the excluded shots' and the key's frame; the new green and blue are held to 4.5:1 with the accent.
    /// </summary>
    [Fact]
    public void ThePlotsMarksHoldTheirContrastInBothThemes()
    {
        var failures = new List<string>();
        foreach (var (name, variant) in new[] { ("light", Avalonia.Styling.ThemeVariant.Light), ("dark", Avalonia.Styling.ThemeVariant.Dark), ("high contrast", Tokens.HighContrastVariant) })
        {
            var inks = Tokens.Plot(variant);
            foreach (var (role, colour, ratio) in new[] { ("ink", inks.Ink, 7.0), ("rings", inks.Ring, 4.5), ("accent", inks.Accent, 4.5), ("group green", inks.Group, 4.5), ("aim blue", inks.Aim, 4.5) })
            {
                if (Contrast(colour, inks.Paper) < ratio)
                {
                    failures.Add($"{name} {role} on the plot's paper: {Contrast(colour, inks.Paper):0.00}, wanted {ratio:0.0}");
                }
            }
        }

        string plot = File.ReadAllText(Path.Combine(Repository(), "src", "GroupLab.App", "CompositePlot.cs")).Replace("OutlineOpacity", "", StringComparison.Ordinal);
        foreach (string faded in new[] { "Faded(", "Opacity", "Marks.Teal", "Marks.Faint" })
        {
            if (plot.Contains(faded, StringComparison.Ordinal))
            {
                failures.Add($"the plot still draws with {faded}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
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
            GroupLab.Tests.Support.Temp.DeleteFile(settings);
        }
    }

    [GeneratedRegex(@"Brushes\.|Colors\.|Color\.(From|Parse)|new Color\(|""#[0-9A-Fa-f]{3,8}""")]
    private static partial Regex ColourLiteral();
}
