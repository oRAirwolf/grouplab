using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 425, a person's report passed on by Alan from nightly 192 on macOS in the dark theme: in a pooled set of 27
/// shots the selected shot was found by clicking it on and off, because its orange was the first sheet's orange, it sat on the red extreme
/// spread line, and in the small sheet and the table it barely changed. The selection now has an ink of its own, a ring and a crosshair,
/// and its row a bar, a fill and bold text.
/// </summary>
public class Entry425Tests
{
    public static TheoryData<string> Themes => new() { "dark", "light", "high contrast" };

    private static ThemeVariant Variant(string theme) => theme switch
    {
        "light" => ThemeVariant.Light,
        "dark" => ThemeVariant.Dark,
        _ => Tokens.HighContrastVariant,
    };

    private static double Distance(Color a, Color b) =>
        Math.Sqrt(Math.Pow(a.R - b.R, 2) + Math.Pow(a.G - b.G, 2) + Math.Pow(a.B - b.B, 2));

    /// <summary>Relative luminance, from the contrast against black that <see cref="Tokens.ContrastRatio"/> gives.</summary>
    private static double Luminance(Color c) => (Tokens.ContrastRatio(c, Colors.Black) * 0.05) - 0.05;

    /// <summary>
    /// Section 4: the selection's ink is not, and is not near, any sheet's color in a pooled set, the bull's, the extreme spread's, the CEP
    /// and group centre's green or the aim's blue, nor the orange it replaces; and its edge is the opposite of it, so one of the two always
    /// shows. Near is closer than 100 in RGB, about the gap between two of the pooled sheets' own colors.
    /// </summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void TheSelectionIsAnInkNothingElseOnThePlotUses(string theme)
    {
        var variant = Variant(theme);
        var (ink, edge) = Tokens.Selection(variant);
        var plot = Tokens.Plot(variant);
        var others = Tokens.Loads(variant).Select((c, i) => ($"sheet {i + 1}", c))
            .Concat([("the bull", plot.Bull), ("the extreme spread", plot.Accent), ("CEP and the group center", plot.Group), ("the aim", plot.Aim),
                ("the orange of what is lit", Tokens.MarkSelected)]);
        var near = others.Where(o => Distance(ink, o.Item2) < 100).Select(o => $"{o.Item1} ({Distance(ink, o.Item2):0})").ToList();
        Assert.True(near.Count == 0, $"In the {theme} theme the selection is close to " + string.Join(", ", near));
        Assert.True(Tokens.ContrastRatio(ink, edge) >= 15, $"the edge is not the opposite of the ink in the {theme} theme");
    }

    /// <summary>
    /// Section 2.1: the selected row is clearly brighter than every other row in the dark themes and clearly darker in the light, and the
    /// text on it keeps full contrast.
    /// </summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void TheSelectedRowStandsApartAndItsTextStaysReadable(string theme)
    {
        var p = Tokens.For(Variant(theme));
        var fill = Tokens.SelectedRow(p);
        bool light = theme == "light";
        foreach (var (name, row) in new[] { ("background", p.Bg), ("panel", p.Panel), ("shaded row", p.Panel2) })
        {
            Assert.True(light ? Luminance(fill) < Luminance(row) : Luminance(fill) > Luminance(row),
                $"In the {theme} theme the selected row is not {(light ? "darker" : "brighter")} than the {name}");
            Assert.True(Tokens.ContrastRatio(fill, row) >= 1.4, $"In the {theme} theme the selected row is only {Tokens.ContrastRatio(fill, row):0.00} to 1 against the {name}");
        }

        double needed = theme == "high contrast" ? Tokens.HighContrastRatio : 7;
        Assert.True(Tokens.ContrastRatio(p.Text, fill) >= needed, $"In the {theme} theme the text on the selected row is {Tokens.ContrastRatio(p.Text, fill):0.0} to 1");
    }

    /// <summary>The small sheet drawn, as BGRA bytes.</summary>
    private static byte[] Drawn(ThemeVariant variant, IReadOnlySet<int> selected)
    {
        const int width = 240, height = 300;
        var thumbnail = new SheetThumbnail
        {
            Width = width,
            Height = height,
            Shots = [new ThumbnailShot(1, new PointD(2.5, 3), false), new ThumbnailShot(2, new PointD(6, 8), false)],
            Selected = selected,
        };
        var window = new Window { Width = width, Height = height, Content = thumbnail, RequestedThemeVariant = variant };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        bitmap.Render(thumbnail);
        window.Close();
        var bgra = new byte[width * height * 4];
        var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, width, height), handle.AddrOfPinnedObject(), bgra.Length, width * 4);
        }
        finally
        {
            handle.Free();
        }

        return bgra;
    }

    /// <summary>
    /// Section 1.2 and section 4: in the small sheet a selected shot's pixels differ plainly from an unselected one's. Before, the dot grew
    /// from 2.4 to 3.2 pixels and turned from red to orange, a few dozen pixels; the ring and crosshair change several hundred.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void TheSmallSheetsSelectedShotIsPlain(string theme)
    {
        var variant = Variant(theme);
        var plain = Drawn(variant, new HashSet<int>());
        var picked = Drawn(variant, new HashSet<int> { 1 });
        int changed = 0;
        for (int i = 0; i < plain.Length; i += 4)
        {
            if (Math.Abs(plain[i] - picked[i]) + Math.Abs(plain[i + 1] - picked[i + 1]) + Math.Abs(plain[i + 2] - picked[i + 2]) > 60)
            {
                changed++;
            }
        }

        Assert.True(changed >= 250, $"selecting a shot on the small sheet changed only {changed} pixels in the {theme} theme");
    }

    /// <summary>
    /// Section 2: a shot picked on the plot marks its row, and only its row, with the selected row's look, its cells bold at full contrast;
    /// and the plot's key says what the new mark is.
    /// </summary>
    [AvaloniaFact]
    public void APickedShotsRowIsMarkedAndTheKeySaysWhatTheRingIs()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var target = window.Plot.Shots[2];
            window.PickShots([target.Id]);
            Dispatcher.UIThread.RunJobs();

            var marked = window.OffsetRows.Where(r => r.Classes.Contains(AppStyles.SelectedRow)).ToList();
            Assert.Single(marked);
            var cells = marked[0].GetLogicalDescendants().OfType<TextBlock>().ToList();
            Assert.NotEmpty(cells);
            Assert.All(cells, c => Assert.Equal(FontWeight.Bold, c.FontWeight));
            Assert.All(window.OffsetRows.Except(marked), r => Assert.DoesNotContain(AppStyles.SelectedRow, r.Classes));
            Assert.Contains(window.Plot.Legend, l => l == "the shot you picked, ringed with a cross");
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }
}
