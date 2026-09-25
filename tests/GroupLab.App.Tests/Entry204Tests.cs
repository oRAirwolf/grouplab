using Avalonia;
using Avalonia.Headless.XUnit;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 204: the composite plot made quieter and easier to read. The figures are the published sample scan's plot
/// in both themes, written under <c>docs/figures/</c> only when <c>GROUPLAB_PLOT_FIGURES</c> names them ("before" or "after").
/// </summary>
public class Entry204Tests
{
    /// <summary>The published sample, scan 3, analyzed as a person would see it, with .308 named so the outlines are drawn.</summary>
    internal static MainWindow Sample()
    {
        string repository = Entry109Tests.Repository();
        var definition = GltdJsonReader.ReadFile(Path.Combine(repository, "targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        string path = Path.Combine(repository, "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.OpenImage(path);
        window.ApplyDetection(result);
        window.Session.SetCalibre(Calibre.Of(0.308));
        window.Session.SetShotDistance(3600);
        window.CalibreAnswered();
        window.Analyse();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void TheSamplePlotFigures()
    {
        if (Environment.GetEnvironmentVariable("GROUPLAB_PLOT_FIGURES") is not { Length: > 0 } which)
        {
            return;
        }

        var window = Sample();
        string store = window.SettingsStore.Path;
        try
        {
            foreach (var (theme, name) in new[] { (ThemeChoice.Light, "light"), (ThemeChoice.Dark, "dark") })
            {
                window.SetTheme(theme);
                Dispatcher.UIThread.RunJobs();
                var plot = window.Plot;
                var size = new PixelSize((int)plot.Bounds.Width, (int)plot.Bounds.Height);
                using var bitmap = new RenderTargetBitmap(size);
                bitmap.Render(plot);
                bitmap.Save(Path.Combine(Entry109Tests.Repository(), "docs", "figures", $"composite-plot-{which}-{name}.png"), new PngBitmapEncoderOptions());
            }
        }
        finally
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
            window.Close();
            File.Delete(store);
        }
    }

    private static double Luminance(Color c)
    {
        static double Linear(byte v) => v / 255.0 <= 0.04045 ? v / 255.0 / 12.92 : Math.Pow(((v / 255.0) + 0.055) / 1.055, 2.4);
        return (0.2126 * Linear(c.R)) + (0.7152 * Linear(c.G)) + (0.0722 * Linear(c.B));
    }

    private static double Contrast(Color a, Color b)
    {
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    /// <summary>
    /// Section 1 and section 2's order: the rings widest and faint, the outlines thin and at half strength, the CEP circles clearly wider
    /// than any outline, and the green, blue and red each clear of the paper in both themes and apart from one another.
    /// </summary>
    [Fact]
    public void TheLayersAreDrawnInTheirOrder()
    {
        Assert.Equal(0.5, CompositePlot.OutlineOpacity);
        Assert.True(CompositePlot.CepStroke >= 1.5 * 1.5, "a CEP circle is not clearly wider than a shot outline");
        Assert.True(CompositePlot.BullStroke > CompositePlot.CepStroke, "the rings are not the widest stroke");
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var inks = Tokens.Plot(variant);
            Assert.True(Contrast(inks.Bull, inks.Paper) < 2, $"{variant}: the rings are not faint");
            foreach (var (name, ink) in new[] { ("green", inks.Group), ("blue", inks.Aim), ("red", inks.Accent) })
            {
                Assert.True(Contrast(ink, inks.Paper) >= 4.5, $"{variant}: the {name} is {Contrast(ink, inks.Paper):0.0}:1 on the paper");
            }

            Assert.NotEqual(inks.Group, inks.Aim);
            Assert.NotEqual(inks.Group, inks.Accent);
            Assert.NotEqual(inks.Aim, inks.Ink);
        }
    }

    /// <summary>Section 1.4: the defaults, each toggle adding and removing exactly its mark in the key, and the choice remembered.</summary>
    [AvaloniaFact]
    public void TheTogglesAddAndRemoveExactlyTheirMark()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;
            Assert.Equal(PlotMarks.Default, plot.Shown);
            Assert.Equal([true, true, false, true], window.PlotToggles.Select(t => t.IsChecked == true));
            Assert.All(window.PlotToggles, t => Assert.True(t.MinHeight >= 44 && t.Focusable, $"{t.Content} is not touch sized or keyboard reachable"));
            var before = plot.Legend.ToList();
            Assert.Contains(before, l => l.StartsWith("CEP 50, the green dotted", StringComparison.Ordinal));
            Assert.Contains(before, l => l.StartsWith("CEP 90, the green solid", StringComparison.Ordinal));
            Assert.DoesNotContain(before, l => l.StartsWith("CEP 95", StringComparison.Ordinal));
            Assert.Contains(before, l => l.StartsWith("extreme spread", StringComparison.Ordinal));
            Assert.Contains("group center, the green lines", before);
            Assert.Contains("where you aimed, the blue lines", before);

            foreach (var (toggle, starts) in window.PlotToggles.Zip(new[] { "CEP 50", "CEP 90", "CEP 95", "extreme spread" }))
            {
                bool was = toggle.IsChecked == true;
                toggle.IsChecked = !was;
                Dispatcher.UIThread.RunJobs();
                var after = plot.Legend.ToList();
                var changed = after.Except(before).Concat(before.Except(after)).ToList();
                Assert.Single(changed);
                Assert.StartsWith(starts, changed[0], StringComparison.Ordinal);
                Assert.Equal(!was, after.Any(l => l.StartsWith(starts, StringComparison.Ordinal)));
                toggle.IsChecked = was;
                Dispatcher.UIThread.RunJobs();
                Assert.Equal(before, plot.Legend);
            }

            window.PlotToggles[2].IsChecked = true;
            window.PlotToggles[3].IsChecked = false;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new PlotMarks(true, true, true, false), window.SettingsStore.LoadPlotMarks());
            Assert.DoesNotContain("extreme spread", MainWindow.ReportPlotCaption(window.Plot.Shown, spread: false), StringComparison.Ordinal);
            Assert.Contains("CEP 95 dashed", MainWindow.ReportPlotCaption(window.Plot.Shown, spread: true), StringComparison.Ordinal);
            Assert.Contains("The extreme spread line is not drawn, as on screen.", MainWindow.ReportPlotCaption(window.Plot.Shown, spread: true), StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    /// <summary>Sections 1.6 and 1.7, rendered: at the plot's left edge the aim point's row is blue and the group centre's row green.</summary>
    [AvaloniaFact]
    public void TheCentreLinesRunTheWholeWidthInTheirColours()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;
            plot.ShowKey = false;
            var size = new PixelSize((int)plot.Bounds.Width, (int)plot.Bounds.Height);
            using var bitmap = new RenderTargetBitmap(size);
            bitmap.Render(plot);
            var inks = Tokens.Plot(plot.ActualThemeVariant);
            Color At(Point p)
            {
                int x = Math.Clamp((int)Math.Round(p.X), 0, size.Width - 1), y = Math.Clamp((int)Math.Round(p.Y), 0, size.Height - 1);
                var buffer = Marshal.AllocHGlobal(4);
                try
                {
                    bitmap.CopyPixels(new PixelRect(x, y, 1, 1), buffer, 4, 4);
                    byte[] px = new byte[4];
                    Marshal.Copy(buffer, px, 0, 4);
                    return Color.FromArgb(px[3], px[2], px[1], px[0]);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            // A line 1.5 wide at a fractional row is shared between two rows of pixels and never covers one whole, so the test is that, of
            // the rows (or columns) about it, one is nearer the expected ink than any other ink the plot draws with.
            Color[] palette = [inks.Paper, inks.Ink, inks.Ring, inks.Accent, inks.Group, inks.Aim, inks.Bull];
            static double Apart(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
            bool Reads(Point p, Color expected, bool rows) => Enumerable.Range(-2, 5).Select(d => At(rows ? new Point(p.X, p.Y + d) : new Point(p.X + d, p.Y)))
                .Any(c => palette.MinBy(k => Apart(c, k)) == expected);
            var aim = plot.ToScreen(new PointD(0, 0));
            var centre = plot.ToScreen(plot.Centre!.Value);
            Assert.True(Reads(new Point(2, aim.Y), inks.Aim, rows: true), $"the aim row at the left edge is {At(new Point(2, aim.Y))}, not {inks.Aim}");
            Assert.True(Reads(new Point(2, centre.Y), inks.Group, rows: true), $"the centre row at the left edge is {At(new Point(2, centre.Y))}, not {inks.Group}");
            Assert.True(Reads(new Point(centre.X, size.Height - 2), inks.Group, rows: false), "the centre column does not reach the bottom edge");
            Assert.True(Reads(new Point(aim.X, 2), inks.Aim, rows: false), "the aim column does not reach the top edge");
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
