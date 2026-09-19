using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 105: the side columns fit and resize, the work bar hides without hiding a failure, the plot names what is
/// under the pointer and keys its marks, the mark is in the header and on the window, calibre names read as their bullets, and sighters are
/// set aside unless analysed.
/// </summary>
public class Entry105Tests
{
    // Two scoring bulls a hundred pixels to the inch apart and a sighter below; five shots on each scoring bull and two on the sighter.
    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(200, 200)), new(1, "2", new PointD(500, 200)), new(2, "S1", new PointD(350, 450), Scoring: false)];

    private static readonly (int X, int Y, int Bull)[] Holes =
    [
        (205, 190, 0), (188, 207, 0), (214, 212, 0), (196, 196, 0), (230, 200, 0),
        (510, 205, 1), (492, 194, 1), (503, 214, 1), (487, 209, 1), (499, 188, 1),
        (360, 440, 2), (344, 458, 2),
    ];

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static (MainWindow Window, AppSettingsStore Store) NewWindow(string? settings = null)
    {
        var store = new AppSettingsStore(settings ?? Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, store);
    }

    private static string Marked(MainWindow window)
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-105-{Guid.NewGuid():N}.png");
        using (var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235)))
        {
            foreach (var (x, y, _) in Holes)
            {
                Cv2.Circle(image, new OpenCvSharp.Point(x, y), 9, new Scalar(30, 30, 30), -1);
            }

            Cv2.ImWrite(path, image);
        }

        window.OpenImage(path);
        window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), Bulls,
            [.. Holes.Select(h => (new PointD(h.X, h.Y), (int?)h.Bull))], "test");
        Dispatcher.UIThread.RunJobs();
        return path;
    }

    /// <summary>
    /// Item 1's defect: at the default width, with no splitter touched, the new-record form's buttons all sit inside the column. Before, the
    /// rifle's button ran past the edge and read "Add rif".
    /// </summary>
    [AvaloniaFact]
    public void TheNewRecordFormFitsTheColumnAtItsDefaultWidth()
    {
        var (window, _) = NewWindow();
        var expander = window.GetLogicalDescendants().OfType<Expander>().Single(e => Equals(e.Header, "New rifle, barrel or load"));
        expander.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var buttons = expander.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Contains(buttons, b => Equals(b.Content, "Add rifle"));
        foreach (var button in buttons)
        {
            var right = button.TranslatePoint(new Avalonia.Point(button.Bounds.Width, 0), expander)!.Value;
            Assert.True(right.X <= expander.Bounds.Width + 0.5, $"\"{button.Content}\" ends at {right.X:0} px in a {expander.Bounds.Width:0} px column");
        }

        window.Close();
    }

    /// <summary>Item 1: each side column has a splitter with the resize cursor and a minimum, and its width is remembered.</summary>
    [AvaloniaFact]
    public void TheSideColumnsHaveSplittersAndTheirWidthsAreRemembered()
    {
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        var (window, store) = NewWindow(settings);
        var splitters = window.GetLogicalDescendants().OfType<GridSplitter>().ToList();
        Assert.Equal(3, splitters.Count);
        Assert.All(splitters, s => Assert.NotNull(s.Cursor));
        foreach (var grid in splitters.Select(s => (Grid)s.Parent!).Distinct())
        {
            Assert.All(grid.ColumnDefinitions.Where(c => c.Width.IsAbsolute), c => Assert.True(c.MinWidth >= 260));
        }

        store.SaveColumnWidth("editor.right", 480);
        window.Close();
        var (later, _) = NewWindow(settings);
        var editor = later.GetLogicalDescendants().OfType<GridSplitter>().Select(s => (Grid)s.Parent!).First(g => g.ColumnDefinitions.Count == 3);
        Assert.Equal(480, editor.ColumnDefinitions[2].Width.Value);
        later.Close();
    }

    /// <summary>
    /// Item 6: Show work shows and hides the bar and is remembered. With the bar hidden, a failed stage is still a prominent error in the panel
    /// and Show work says a stage failed, so the trace is never the only place it appears (DESIGN.md section 19).
    /// </summary>
    [AvaloniaFact]
    public void AFailedStageIsVisibleWhileTheWorkBarIsHidden()
    {
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        var (window, _) = NewWindow(settings);
        Assert.False(window.WorkBar.Shown);

        var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var blank = new GrayImage(1275, 1650, Enumerable.Repeat((byte)240, 1275 * 1650).ToArray());
        var trace = new TraceRecorder();
        var result = AutomaticMarking.Run(blank, blank, ImageMetadata.ForScan(1275, 1650, 150), definition, new OpenCvSharpBackend(), trace);
        window.ApplyDetection(result);
        window.ShowTrace(trace.Records);
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.WorkBar.Shown);
        var error = window.GetLogicalDescendants().OfType<TextBlock>().Single(t => (t.Text ?? "").StartsWith("Detection failed", StringComparison.Ordinal));
        Assert.True(error.IsEffectivelyVisible);
        Assert.Contains("failed", window.WorkBar.Label, StringComparison.Ordinal);

        window.SetShowWork(true);
        Assert.True(window.WorkBar.Shown);
        Assert.Equal("Show work", window.WorkBar.Label);
        window.Close();
        var (later, _) = NewWindow(settings);
        Assert.True(later.WorkBar.Shown);
        later.Close();
    }

    /// <summary>
    /// Item 8 in the window: with sighters set aside the counts are the scoring shots, the sighter marks are drawn set aside, and there is no
    /// sighter section; analysed, the counts include them and they have a section of their own. The switch is remembered.
    /// </summary>
    [AvaloniaFact]
    public void SightersAreSetAsideUntilAnalysed()
    {
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        var (window, store) = NewWindow(settings);
        string path = Marked(window);
        try
        {
            Assert.Contains("10 shots", window.BreadcrumbText, StringComparison.Ordinal);
            Assert.Equal(2, window.Canvas.SetAside.Count);
            Assert.DoesNotContain(window.FigureColumnHeadings, h => h.StartsWith("Sighters", StringComparison.Ordinal));

            var box = window.GetLogicalDescendants().OfType<CheckBox>().Single(c => Equals(c.Content, "Analyse sighters"));
            Assert.True(box.IsVisible);
            box.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("12 shots", window.BreadcrumbText, StringComparison.Ordinal);
            Assert.Empty(window.Canvas.SetAside);
            Assert.Contains(window.FigureColumnHeadings, h => h.StartsWith("Sighters", StringComparison.Ordinal));
            Assert.True(store.LoadAnalyseSighters());

            // The scoring group never counts them either way.
            Assert.Equal(10, window.Plot.Shots.Count);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Item 3: the plot names what is under the pointer, a shot with its bull, an excluded shot as excluded, the extreme spread as a distance
    /// between two shots, the CEP circles by what they mean, and nothing on empty background; and its key names each mark it draws.
    /// </summary>
    [AvaloniaFact]
    public void ThePlotNamesWhatIsUnderThePointerAndKeysItsMarks()
    {
        var (window, _) = NewWindow();
        string path = Marked(window);
        try
        {
            var shots = window.Session.State.Shots;
            window.Session.SetExclusion(shots[4].Id, ExclusionReason.PulledShot);
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;

            var first = plot.Shots.First(s => !s.Excluded);
            string shot = plot.Describe(plot.ToScreen(first.Offset))!;
            Assert.StartsWith($"Shot {first.Label}, bull {first.Bull}.", shot, StringComparison.Ordinal);
            Assert.Contains("from the group centre", shot, StringComparison.Ordinal);

            var excluded = plot.Shots.Single(s => s.Excluded);
            Assert.Contains("Excluded", plot.Describe(plot.ToScreen(excluded.Offset)), StringComparison.Ordinal);

            var (a, b) = plot.SpreadPair!.Value;
            var pa = plot.ToScreen(plot.Shots.Single(s => s.Id == a).Offset);
            var pb = plot.ToScreen(plot.Shots.Single(s => s.Id == b).Offset);
            string? spread = Enumerable.Range(1, 19)
                .Select(i => plot.Describe(new Avalonia.Point(pa.X + ((pb.X - pa.X) * i / 20.0), pa.Y + ((pb.Y - pa.Y) * i / 20.0))))
                .FirstOrDefault(d => d?.StartsWith("Extreme spread", StringComparison.Ordinal) == true);
            Assert.Contains("not a region", spread, StringComparison.Ordinal);

            var centre = plot.ToScreen(plot.Centre!.Value);
            double r90 = plot.Cep90Inches!.Value * (plot.ToScreen(new PointD(1, 0)).X - plot.ToScreen(new PointD(0, 0)).X);
            string? cep = new[] { 0.0, 1, 2, 3 }.Select(k => plot.Describe(new Avalonia.Point(centre.X + r90 + k, centre.Y))).FirstOrDefault(d => d?.StartsWith("CEP 90", StringComparison.Ordinal) == true);
            Assert.Contains("nine times in ten", cep, StringComparison.Ordinal);

            Assert.Null(plot.Describe(new Avalonia.Point(plot.Bounds.Width - 2, plot.Bounds.Height - 2)));
            Assert.Contains(plot.Legend, l => l == "CEP 50, the dotted circle");
            Assert.Contains(plot.Legend, l => l == "CEP 90, the dashed circle");
            Assert.Contains(plot.Legend, l => l == "1 excluded, drawn hollow");
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Entry 107 section 1 in the window: a name is refused with the sentence that says what to type and sets nothing, and a diameter is set and
    /// shown in both units, as the load panel shows it.
    /// </summary>
    [AvaloniaFact]
    public void TheCalibreBoxTakesADiameterAndRefusesAName()
    {
        var (window, _) = NewWindow();
        string path = Marked(window);
        try
        {
            window.EnterCalibre("38 Cal.");
            Assert.Null(window.Session.State.Calibre);
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == Calibre.Refusal);

            window.EnterCalibre(".357");
            Assert.Equal(0.357, window.Session.State.Calibre!.DiameterInches, 12);
            Assert.Equal(".357 in (9.07 mm)", window.Session.State.Calibre.Name);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Items 4 and 5: the header carries the lockup, the rail the mark, each drawn from the committed SVG for the theme showing; the window
    /// carries the icon, and the icon files the build and the other platforms need exist.
    /// </summary>
    [AvaloniaFact]
    public void TheMarkIsInTheHeaderTheRailAndTheWindow()
    {
        var (window, _) = NewWindow();
        Assert.NotNull(window.Icon);
        var marks = window.GetLogicalDescendants().OfType<BrandMark>().ToList();
        Assert.Contains(marks, m => m.Lockup && m.Art.Shapes.Count == 7);
        Assert.Contains(marks, m => !m.Lockup && m.Art.Shapes.Count == 5);

        window.SetTheme(ThemeChoice.Light);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.GetLogicalDescendants().OfType<BrandMark>(), m => m.Source == "grouplab-lockup-light.svg");
        window.SetTheme(ThemeChoice.Dark);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.GetLogicalDescendants().OfType<BrandMark>(), m => m.Source == "grouplab-lockup.svg");
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;

        string icons = Path.Combine(Repository(), "src", "GroupLab.App", "Assets", "icons");
        Assert.True(File.Exists(Path.Combine(icons, "grouplab.ico")));
        Assert.True(File.Exists(Path.Combine(icons, "grouplab.icns")));
        Assert.All(GroupLab.Cli.IconSet.LinuxSizes, s => Assert.True(File.Exists(Path.Combine(icons, "linux", $"grouplab-{s}.png")), $"{s} px"));

        // Six images in the icon file, one a size, the 256 pixel one written as 0 as the format asks.
        byte[] ico = File.ReadAllBytes(Path.Combine(icons, "grouplab.ico"));
        Assert.Equal(GroupLab.Cli.IconSet.WindowsSizes.Length, BitConverter.ToUInt16(ico, 4));
        Assert.Equal([16, 24, 32, 48, 64, 0], Enumerable.Range(0, 6).Select(i => (int)ico[6 + (16 * i)]));
        window.Close();
    }

    /// <summary>
    /// Item 2: each figure row is one shape, the value alone beside its label with the angle on the line beneath, and the zero readouts are
    /// cells in columns rather than text laid out on a line.
    /// </summary>
    [AvaloniaFact]
    public void FigureRowsShareOneShapeAndTheZeroReadoutsAlign()
    {
        var (window, _) = NewWindow();
        string path = Marked(window);
        try
        {
            window.Session.SetShotDistance(3600);
            Dispatcher.UIThread.RunJobs();
            var text = window.StatisticsText.ToList();
            int meanRadius = text.IndexOf("Mean radius");
            Assert.EndsWith(" in", text[meanRadius + 1], StringComparison.Ordinal);
            Assert.Contains("MOA", text[meanRadius + 2], StringComparison.Ordinal);

            var zero = window.ZeroText.ToList();
            Assert.Contains(zero, t => t is "left" or "right");
            Assert.Contains(zero, t => t is "high" or "low");
            Assert.Contains(zero, t => t.EndsWith(" MOA", StringComparison.Ordinal) && !t.Contains(' ' + "in", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
