using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 103 sections 1 and 2: the one screen as the two the concept shows. The editor goes forward by Accept and
/// analyse and comes back by the sheet crumb with every edit intact; the analysis state carries the composite plot, the figures the stack
/// lacked, and two judgement cards that neither name the wrong test nor print a negative without its power.
/// </summary>
public class AnalysisStateTests
{
    // Two scoring bulls a hundred pixels to the inch apart and a sighter below them; five shots on each scoring bull and one on the sighter.
    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(200, 200)), new(1, "2", new PointD(500, 200)), new(2, "S1", new PointD(350, 450), Scoring: false)];

    private static readonly (int X, int Y, int Bull)[] Holes =
    [
        (205, 190, 0), (188, 207, 0), (214, 212, 0), (196, 196, 0), (230, 200, 0),
        (510, 205, 1), (492, 194, 1), (503, 214, 1), (487, 209, 1), (499, 188, 1),
        (360, 440, 2),
    ];

    private static MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>A marked sheet: the image written to a temporary file, opened, and the shots loaded on their bulls.</summary>
    private static (MainWindow Window, string Path) Marked()
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-analysis-{Guid.NewGuid():N}.png");
        using (var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235)))
        {
            foreach (var (x, y, _) in Holes)
            {
                Cv2.Circle(image, new OpenCvSharp.Point(x, y), 9, new Scalar(30, 30, 30), -1);
            }

            Cv2.ImWrite(path, image);
        }

        var window = NewWindow();
        window.Show();
        window.OpenImage(path);
        window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), Bulls,
            [.. Holes.Select(h => (new PointD(h.X, h.Y), (int?)h.Bull))], "test");
        Dispatcher.UIThread.RunJobs();
        return (window, path);
    }

    private static string DefinitionPath([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "targets", "GL-CF25-LTR.gltd.json"));

    private static GroupLab.Core.Gltd.Model.TargetDefinition Definition() => GltdJsonReader.ReadFile(DefinitionPath()).Definition!;

    private static Button Named(MainWindow window, string content) =>
        window.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, content));

    [AvaloniaFact]
    public void AcceptGoesForwardAndTheSheetCrumbComesBackWithEveryEditIntact()
    {
        var (window, path) = Marked();
        try
        {
            string editorPill = window.PillText;
            Assert.EndsWith("need review", editorPill, StringComparison.Ordinal);
            int moved = window.Session.State.Shots[0].Id;
            window.Session.MoveShot(moved, new PointD(207, 192));
            var edited = window.Session.State;

            // Entry 131 section 6.3 holds Accept until the calibre question is answered; this test is about the crumb, not the calibre.
            window.CalibreAnswered();
            Named(window, "Accept and analyze").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.Analysing);
            Assert.Equal("\u26a0 Scale set by hand", window.PillText);

            // The breadcrumb's sheet crumb is the file's name, and one click goes back to the marks with nothing lost.
            var crumb = Named(window, Path.GetFileName(path));
            crumb.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.Analysing);
            Assert.Same(edited, window.Session.State);
            Assert.Equal(editorPill, window.PillText);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>A person may accept with items open, and then the analysis says how many decisions every figure inherits.</summary>
    [AvaloniaFact]
    public void AcceptingWithItemsOpenNamesThemAboveTheFigures()
    {
        var (window, path) = Marked();
        try
        {
            // The marking opens with five shots on each of two bulls, two items a person has not looked at, and the line names both.
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            int open = ReviewQueue.Open(ReviewQueue.For(window.Session.State));
            Assert.True(open > 1);
            Assert.StartsWith($"{open} decisions left unmade", window.UnsettledText, StringComparison.Ordinal);
            Assert.Contains(window.UnsettledLines, l => l.StartsWith($"{open} decisions were left unmade when this was accepted", StringComparison.Ordinal));

            foreach (var item in ReviewQueue.For(window.Session.State).Where(i => !i.Resolved))
            {
                window.Session.Dismiss(item.Key);
            }

            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", window.UnsettledText);

            // Twelve rounds fired against ten marks on the scoring bulls: one decision left unmade.
            window.Session.SetExpectedShots(12);
            Dispatcher.UIThread.RunJobs();
            Assert.StartsWith("1 decision left unmade", window.UnsettledText, StringComparison.Ordinal);
            Assert.Contains(window.UnsettledLines, l => l.Contains("every figure here inherits it", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>
    /// The plot's content rules: sighters are not in it, excluded shots are drawn and marked excluded, marks set to not a shot are absent,
    /// and a shot is a point with no calibre and a circle at the calibre's diameter with one, the legend saying which.
    /// </summary>
    [AvaloniaFact]
    public void ThePlotHoldsTheScoringShotsOnlyAndKeepsTheExcludedOnes()
    {
        var (window, path) = Marked();
        try
        {
            var shots = window.Session.State.Shots;
            int sighterShot = shots.Single(s => s.Bull == 2).Id;
            int excluded = shots[4].Id, notAShot = shots[9].Id;
            window.Session.SetExclusion(excluded, ExclusionReason.PulledShot);
            window.Session.SetNotAShot(notAShot, true);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();

            var plotted = window.Plot.Shots;
            Assert.DoesNotContain(plotted, p => p.Id == sighterShot);
            Assert.DoesNotContain(plotted, p => p.Id == notAShot);
            Assert.True(plotted.Single(p => p.Id == excluded).Excluded);
            Assert.Equal(9, plotted.Count);

            // Each shot from its own bull's aim point: the first shot is 5 px right and 10 px up of bull 1, 0.05 and 0.10 in at 100 px per inch.
            var first = plotted.Single(p => p.Id == shots[0].Id).Offset;
            Assert.Equal(0.05, first.X, 6);
            Assert.Equal(-0.10, first.Y, 6);

            Assert.Contains(window.Plot.Legend, l => l.Contains("drawn as points", StringComparison.Ordinal));
            Assert.Contains(window.Plot.Legend, l => l == "1 excluded, drawn hollow");
            Assert.Null(window.Plot.CalibreInches);
            window.Session.SetCalibre(new Calibre(".308", 0.308));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0.308, window.Plot.CalibreInches);
            Assert.Contains(window.Plot.Legend, l => l.Contains("drawn at the 0.308 in caliber", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>A shot clicked on the plot selects it, as the shot list does; the extreme spread line clicked picks both its shots.</summary>
    [AvaloniaFact]
    public void AClickOnThePlotSelectsAShotAndTheSpreadLinePicksBoth()
    {
        var (window, path) = Marked();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var plot = window.Plot;
            Assert.True(plot.Bounds.Width > 0, "the plot was not laid out");

            var target = plot.Shots[2];
            Assert.Equal([target.Id], plot.Pick(plot.ToScreen(target.Offset)));
            window.PickShots(plot.Pick(plot.ToScreen(target.Offset)));
            Assert.Equal(target.Id, window.Canvas.Selected);

            var (a, b) = plot.SpreadPair!.Value;
            var pa = plot.ToScreen(plot.Shots.Single(s => s.Id == a).Offset);
            var pb = plot.ToScreen(plot.Shots.Single(s => s.Id == b).Offset);
            // A point on the line clear of every shot's own reach: the line's pick, not a shot's.
            var picked = Enumerable.Range(1, 19)
                .Select(i => plot.Pick(new Avalonia.Point(pa.X + ((pb.X - pa.X) * i / 20.0), pa.Y + ((pb.Y - pa.Y) * i / 20.0))))
                .First(p => p.Count == 2);
            Assert.Equal(new HashSet<int> { a, b }, picked.ToHashSet());
            window.PickShots(picked);
            Assert.Equal(new HashSet<int> { a, b }, window.PlotSelection.ToHashSet());
            Assert.Contains(plot.Legend, l => l.StartsWith("extreme spread, shots", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>
    /// Section 2: the round card names the circularity test, the likelihood ratio, and not Pitman-Morgan, which answers the stringing question
    /// and is labelled as that; a card with no evidence of stringing carries what the shot count could have detected; the flyer card keeps
    /// "by that measure alone". The stack gains CEP and width by height after the existing figures, with mean radius still first.
    /// </summary>
    [AvaloniaFact]
    public void TheCardsNameTheirTestsAndNeverPrintABareNegative()
    {
        var (window, path) = Marked();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var cards = window.JudgementCards;
            Assert.Equal(2, cards.Count);

            var shape = cards[0];
            Assert.Contains("likelihood ratio", shape[1], StringComparison.Ordinal);
            Assert.StartsWith("Circularity test", shape[1], StringComparison.Ordinal);
            Assert.DoesNotContain("Pitman-Morgan", shape[0], StringComparison.Ordinal);
            Assert.DoesNotContain("Pitman-Morgan", shape[1], StringComparison.Ordinal);
            string stringing = shape.Single(l => l.Contains("Pitman-Morgan", StringComparison.Ordinal));
            Assert.StartsWith("Vertical stringing", stringing, StringComparison.Ordinal);
            if (stringing.Contains("no evidence of vertical stringing", StringComparison.Ordinal))
            {
                Assert.Contains("would be missed", stringing, StringComparison.Ordinal);
                Assert.Contains("needs about 19 shots", stringing, StringComparison.Ordinal);
            }

            var flyer = cards[1];
            Assert.StartsWith("Shot ", flyer[0], StringComparison.Ordinal);
            Assert.Contains(flyer, l => l.Contains("by that measure alone", StringComparison.Ordinal));
            Assert.Contains(flyer, l => l.Contains("STATISTICS.md section 10", StringComparison.Ordinal));

            var text = window.StatisticsText.ToList();
            int meanRadius = text.IndexOf("Mean radius"), cep = text.IndexOf("CEP 90"), size = text.IndexOf("Group width × height");
            // Entry 169 section 1: in the order a shooter reads them, width by height, then mean radius, then the CEPs.
            Assert.True(size >= 0 && meanRadius > size && cep > meanRadius, string.Join(" | ", text));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(path);
        }
    }

    /// <summary>A sheet the detector registered: the plot draws that sheet's own bull behind the shots, from its definition.</summary>
    [AvaloniaFact]
    public void ADetectedSheetsPlotDrawsItsOwnBull()
    {
        var definition = Definition();
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(103);
        // A hole on every scoring bull, each at its own offset, so the composite is a group rather than one point.
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        var image = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(image, image, ImageMetadata.ForScan(image.Width, image.Height, dpi), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);

        var window = NewWindow();
        window.Show();
        window.ApplyDetection(result);
        window.CalibreAnswered();
        window.Analyse();
        Dispatcher.UIThread.RunJobs();
        var ring = definition.RingSets.Single(r => r.Key == definition.Bulls.First(b => b.Scoring).RingSet);
        Assert.Equal(ring.Discs.Count, window.Plot.Discs.Count);
        Assert.Equal(ring.Discs[0].Diameter / 254.0, window.Plot.Discs[0].DiameterInches, 9);
        Assert.DoesNotContain(window.Plot.Legend, l => l.StartsWith("no bull drawn", StringComparison.Ordinal));

        // Entry 104 section 3: the fixture carries what the zero correction reads in clicks, a distance and a rifle, so the block Alan asked to
        // be prominent is exercised, and it stands above the group figures where entry 92 put it.
        window.Session.SetCalibre(new Calibre(".308", 0.308));
        window.Session.SetShotDistance(3600);
        window.Session.SetEquipment(new Rifle("Test rifle", 0.25, GroupLab.Core.Statistics.AngularUnit.Moa), null, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.ZeroText, t => t.StartsWith("In clicks of Test rifle's scope", StringComparison.Ordinal));
        var headings = window.FigureColumnHeadings.ToList();
        Assert.True(headings.IndexOf("Zero correction") >= 0 && headings.IndexOf("Zero correction") < headings.IndexOf("Group"), string.Join(" | ", headings));

        // Photographed in each theme the test sets, named after it, as ScreenshotTests does, for a person to compare with the concept.
        string output = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(DefinitionPath())!, "..", "out", "screens"));
        Directory.CreateDirectory(output);
        try
        {
            foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
            {
                window.SetTheme(theme);
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                window.CaptureRenderedFrame()!.Save(Path.Combine(output, $"analysis-state-{name}.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }
        }
        finally
        {
            Avalonia.Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
        }

        window.Close();
    }
}
