using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
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
/// NOTES-FROM-PLANNING.md entry 109: the layout and readability pass on the marking and analysis screens. Nothing here asserts a finding's
/// meaning, which the tests of entries 91 to 108 already hold; these hold the shape: five text sizes, the header's actions, the task panel
/// without the settings, the reasoning behind "why", and the table and plot as entry 109 section 3 draws them. The last test photographs every
/// screen for a person to compare with docs/figures/screens.
/// </summary>
public class Entry109Tests
{
    internal static string Repository([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    /// <summary>
    /// GL-CF25-LTR rendered at 300 DPI with a hole on every scoring bull, written to a file and opened, and detected by the real pipeline, so
    /// every screen shows what a person would see after opening a scan.
    /// </summary>
    internal static (MainWindow Window, string Path, AutomaticResult Result) Sheet(int width = 1400, int height = 900)
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(109);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        var image = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry109-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "gl-cf25-ltr-scan.png");
        using (var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels))
        {
            Cv2.ImWrite(path, mat);
        }

        var (grey, metadata) = ImageLoader.Load(path);
        var (value, _) = ImageLoader.LoadMaxChannel(path);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);

        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = width, Height = height };
        window.Show();
        window.OpenImage(path);
        window.ApplyDetection(result);
        Dispatcher.UIThread.RunJobs();
        return (window, path, result);
    }

    /// <summary>Whether a control is showing: it and every logical ancestor visible, which holds before a hidden panel's template is applied.</summary>
    internal static bool Shown(Control control) => control.GetSelfAndLogicalAncestors().OfType<Control>().All(c => c.IsVisible);

    /// <summary>Every text in the window's logical tree, with the size it is drawn at.</summary>
    private static IEnumerable<TextBlock> Texts(Visual root) => root.GetLogicalDescendants().OfType<TextBlock>();

    /// <summary>Entry 109 section 1 principle 2: five text styles and no more, with mean radius's lead figure the one named exception.</summary>
    [AvaloniaFact]
    public void EveryTextOnTheScreensIsOneOfTheFiveStyles()
    {
        var (window, path, _) = Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            void Check(string screen)
            {
                Dispatcher.UIThread.RunJobs();
                var off = Texts(window).Where(t => !Tokens.TypeScale.Contains(t.FontSize)).Select(t => $"{t.Text} at {t.FontSize}").ToList();
                Assert.True(off.Count == 0, $"{screen}: text off the type scale: {string.Join(" | ", off)}");
            }

            Check("marking");
            window.Analyse();
            window.SetEveryWhy(true);
            Check("analysis");
            window.ShowSettings();
            Check("settings");
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 109 section 2a: the header ends with the pill, Detect, Show work, Discard edits, Accept and analyse and one menu, which holds the
    /// document's actions and Report a problem; the tools are icons alone, each named with its key; Print is the rail's, not a button.
    /// </summary>
    [AvaloniaFact]
    public void TheHeaderIsOneLineAndTheToolsAreIconsNamedWithTheirKeys()
    {
        var (window, path, _) = Sheet();
        try
        {
            Assert.Equal(["Open image…", "Open marking…", "Export…", "Report a problem…"], window.MenuItems);
            var words = window.GetLogicalDescendants().OfType<Button>().Select(b => b.Content as string).Where(c => c is not null).ToList();
            foreach (string gone in new[] { "Open image", "Open marking", "Print a target", "Report a problem", "Zoom in", "Zoom out", "Fit", "Undo", "Redo" })
            {
                Assert.DoesNotContain(gone, words);
            }

            Assert.Contains("Detect on a GroupLab sheet", words);
            Assert.Contains("Accept and analyse", words);
            var tools = window.GetLogicalDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Where(t => t.Classes.Contains(AppStyles.IconButton)).ToList();
            Assert.Equal(6, tools.Count);
            Assert.All(tools, t => Assert.IsType<PathIcon>(t.Content));
            Assert.Equal(["Pan (P)", "Scale: length (L)", "Scale: rectangle (R)", "Point of aim (A)", "Impact (I)", "Select (V)"], tools.Select(t => ToolTip.GetTip(t) as string));
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 109 sections 2b to 2e: the panel is the task, the settings are their own screen, the scale is one line with its detail behind
    /// "why", and the status line says in one line what detection did.
    /// </summary>
    [AvaloniaFact]
    public void ThePanelIsTheTaskAndTheSettingsAreTheirOwnScreen()
    {
        var (window, path, result) = Sheet();
        try
        {
            var headings = window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(AppStyles.Section) && Shown(t)).Select(t => t.Text).ToList();
            Assert.DoesNotContain("Units", headings);
            Assert.DoesNotContain("Theme", headings);
            Assert.DoesNotContain("Diagnostics", headings);
            Assert.True(headings.IndexOf("Review") < headings.IndexOf("Selected shot") && headings.IndexOf("Selected shot") < headings.IndexOf("Scale"), string.Join(" | ", headings));

            var scale = window.ScaleInputs.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            int found = result.Measurement.Fiducials!.Matches.Count, expected = result.Measurement.Fiducials.Expected;
            Assert.Contains($"Scale from the printed markers, {found} of {expected}", scale);
            Assert.Contains(scale, t => t.StartsWith("From the sheet's own printed markers:", StringComparison.Ordinal));
            Assert.DoesNotContain(scale, t => t.Contains("OneToOne", StringComparison.Ordinal) || t.EndsWith(": .", StringComparison.Ordinal));

            Assert.StartsWith($"Detected {result.Detections.Count} holes on {result.Bulls.Count(b => b.Scoring)} bulls", window.StatusText, StringComparison.Ordinal);

            window.ShowSettings();
            Dispatcher.UIThread.RunJobs();
            var settings = window.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(AppStyles.Section) && Shown(t)).Select(t => t.Text).ToList();
            // Entry 119 section 4 adds "This build", which is where a tester reads the version and the commit for a bug report.
            Assert.Equal(["Units", "Theme", "This build", "Diagnostics", "Crash records"], settings);
            window.ShowSettings(false);
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 109 section 3: the reasoning is one click away and closed by default, and the table shows one number per shot with plain rows.
    /// The stringing line with its power statement stays in view.
    /// </summary>
    [AvaloniaFact]
    public void TheAnalysisShowsOneLinePerFactAndTheReasoningBehindWhy()
    {
        var (window, path, _) = Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            // Entry 111 section 3: each "why" is a small button beside its item, and what it opens is hidden until it is opened.
            var bodies = window.GetLogicalDescendants().OfType<StackPanel>().Where(b => b.Classes.Contains(AppStyles.WhyBody) && Shown((Control)b.Parent!)).ToList();
            Assert.NotEmpty(bodies);
            Assert.All(bodies, b => Assert.False(b.IsVisible));

            var shape = window.JudgementCards[0];
            // The lines in view, the verdict first: every text in the card that is not inside a "why".
            var shown = window.GetLogicalDescendants().OfType<Border>().Single(b => b.Name == "shapeCard").GetLogicalDescendants().OfType<TextBlock>()
                .Where(t => !t.GetLogicalAncestors().OfType<StackPanel>().Any(a => a.Classes.Contains(AppStyles.WhyBody))).Select(t => t.Text ?? "").ToList();
            Assert.StartsWith("Circularity test, ", shown[1], StringComparison.Ordinal);
            Assert.Contains(": p = ", shown[1], StringComparison.Ordinal);
            Assert.StartsWith("Vertical stringing", shown[2], StringComparison.Ordinal);
            Assert.Contains(shape, l => l.StartsWith("Error ellipse", StringComparison.Ordinal));

            var zero = window.ZeroText.ToList();
            Assert.Contains(zero, t => t.StartsWith("Not distinguishable from zero at ", StringComparison.Ordinal) || t.StartsWith("Dial", StringComparison.Ordinal));

            // One number per shot: shots are named by their bull, so no bull column when every shot sits on its own.
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "bull" && Shown(t));
            Assert.Contains(window.Plot.Legend, l => l.Contains("a dot at each centre", StringComparison.Ordinal));
            window.Plot.ShowOutlines = false;
            Assert.Contains(window.Plot.Legend, l => l.Contains("outlines are hidden", StringComparison.Ordinal));

            window.SetEveryWhy(true);
            Assert.All(window.GetLogicalDescendants().OfType<StackPanel>().Where(b => b.Classes.Contains(AppStyles.WhyBody)), b => Assert.True(b.IsVisible));
            window.Close();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    /// <summary>
    /// Entry 109 section 4: every screen, dark and light, at 1400 by 900 and at 1920 by 1080, and the analysis with its disclosures closed and
    /// open, for Alan and planning to judge by looking. They are written to out/screens/current, and to docs/figures/screens/current as well
    /// when GROUPLAB_SCREENS_TO_DOCS is set, which is how the committed set is regenerated. Nothing here asserts what they look like, and no
    /// drift test reads them, since pixel output varies between machines.
    /// </summary>
    [AvaloniaFact]
    public void EveryScreenIsPhotographedInBothThemesAtBothSizes()
    {
        var outputs = new List<string> { Path.Combine(Repository(), "out", "screens", "current") };
        if (Environment.GetEnvironmentVariable("GROUPLAB_SCREENS_TO_DOCS") is { Length: > 0 })
        {
            outputs.Add(Path.Combine(Repository(), "docs", "figures", "screens", "current"));
        }

        foreach (string output in outputs)
        {
            Directory.CreateDirectory(output);
        }

        void Save(Avalonia.Controls.Window window, string name)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            foreach (string output in outputs)
            {
                frame.Save(Path.Combine(output, name + ".png"), new PngBitmapEncoderOptions());
            }
        }

        try
        {
            foreach (var (width, height) in new[] { (1400, 900), (1920, 1080) })
            {
                var (window, path, _) = Sheet(width, height);
                try
                {
                    window.Session.SetCalibre(Calibre.Of(0.308));
                    window.Session.SetShotDistance(3600);
                    // Entry 112 section 4: a rifle and load carrying what the solver needs, so the Ballistics screen shows its table.
                    var rifle = new Rifle("Test rifle", 0.25, GroupLab.Core.Statistics.AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };
                    window.Book = RecordBook.Empty.With(rifle).With(new Load("Test load", null)
                    {
                        MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = GroupLab.Core.Ballistics.DragModel.G7,
                        BcReference = GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
                    });
                    window.Session.SetEquipment(rifle, null, "Test load");
                    var print = new PrintWindow { Width = width, Height = height };
                    print.Show();
                    print.Select("GL-CF25-LTR.gltd.json");
                    foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                    {
                        string size = $"{width}x{height}";
                        window.SetTheme(theme);
                        window.BackToEditor();
                        window.Canvas.FitToView();
                        Save(window, $"marking-{name}-{size}");
                        window.Analyse();
                        window.SetEveryWhy(false);
                        Save(window, $"analysis-{name}-{size}");
                        window.SetEveryWhy(true);
                        Save(window, $"analysis-open-{name}-{size}");
                        window.SetEveryWhy(null);
                        window.ShowSettings();
                        Save(window, $"settings-{name}-{size}");
                        window.ShowSettings(false);
                        window.ShowSessions();
                        Save(window, $"sessions-{name}-{size}");
                        window.ShowSessions(false);
                        window.ShowLibrary();
                        Save(window, $"library-{name}-{size}");
                        window.ShowLibrary(false);
                        window.ShowBallistics();
                        window.ProjectGroup("600", 0, "4", "4", "2");
                        Save(window, $"ballistics-{name}-{size}");
                        window.ShowBallistics(false);

                        // Entry 113 section 2: two sessions of the sheet compared, the second saved as another load the first time round.
                        if (window.Sessions!.List().Count < 2)
                        {
                            long saved = window.CurrentSession!.Value;
                            long copy = window.Sessions.Save(window.Sessions.Get(saved)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Load = "Second load" });
                            window.ChooseSession(saved, true);
                            window.ChooseSession(copy, true);
                        }

                        window.CompareChosen();
                        Save(window, $"compare-{name}-{size}");
                        window.ShowCompare(false);
                        Save(print, $"print-{name}-{size}");
                    }

                    print.Close();
                    window.Close();
                }
                finally
                {
                    Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
                }
            }
        }
        finally
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
        }
    }
}
