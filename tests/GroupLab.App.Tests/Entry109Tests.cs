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
    /// Alan's two 6 ARC scans of 2026-09-26, from the test-data release that GROUPLAB_TEST_DATA names, or null without it. Only the release,
    /// whose files carry their consent records and hashes: PublishedRendersTests refuses anything that publishes reading from elsewhere.
    /// </summary>
    private static (string Dominus, string Magnus)? SuppressorScans()
    {
        string? folder = Environment.GetEnvironmentVariable("GROUPLAB_TEST_DATA");
        string dominus = Path.Combine(folder ?? "", "load-sheet-6arc-dominus-k-2026-09-26.png");
        string magnus = Path.Combine(folder ?? "", "load-sheet-6arc-magnus-m-2026-09-26.png");
        return folder is { Length: > 0 } && File.Exists(dominus) && File.Exists(magnus) ? (dominus, magnus) : null;
    }

    /// <summary>One scan opened, detected, analyzed and saved as a session under <paramref name="load"/>; the session's id.</summary>
    private static long AnalyzeScan(MainWindow window, string scan, string load)
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR-D.gltd.json")).Definition!;
        var (grey, metadata) = ImageLoader.Load(scan);
        var (value, _) = ImageLoader.LoadMaxChannel(scan);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(0.243));
        Assert.Null(result.Failure);
        window.OpenImage(scan);
        window.ApplyDetection(result);
        window.Session.SetCalibre(Calibre.Of(0.243));
        window.Session.SetShotDistance(3600);
        window.Session.SetEquipment(new Rifle("6 ARC", 0.1, GroupLab.Core.Statistics.AngularUnit.Mil), null, load);
        window.CalibreAnswered();
        window.Analyse();
        Dispatcher.UIThread.RunJobs();
        return window.CurrentSession!.Value;
    }

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
            window.CalibreAnswered();
            window.Analyse();
            window.SetEveryWhy(true);
            Check("analysis");
            window.ShowSettings();
            Check("settings");
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
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
            // Entry 140 section 2 put New target at the head of the menu: it is the document action that comes before opening one. Entry 137
            // section 5 put Paste under Open image, because it is the same act with a different source.
            Assert.Equal(
                [$"New target ({CommandKey.Label("N")})", "Open image…", $"Paste an image ({CommandKey.Label("V")})", "Open marking…", "Export…", "Share a session file…", "Open a session file…", "Import shots from a CSV…", "Report a problem…"],
                window.MenuItems);
            // Only what is on this screen: the library has worded Zoom in, Zoom out and Fit buttons of its own (entry 120 section 10.3),
            // and they are in the window's tree whichever screen is showing.
            var words = window.GetLogicalDescendants().OfType<Button>().Where(Shown).Select(b => b.Content as string).Where(c => c is not null).ToList();
            foreach (string gone in new[] { "Open image", "Open marking", "Print a target", "Report a problem", "Zoom in", "Zoom out", "Fit", "Undo", "Redo" })
            {
                Assert.DoesNotContain(gone, words);
            }

            Assert.Contains("Detect on a GroupLab sheet", words);
            Assert.Contains("Accept and analyze", words);
            var tools = window.GetLogicalDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Where(t => t.Classes.Contains(AppStyles.IconButton)).ToList();
            // Entry 228 added the bulls tool and the lasso, for targets GroupLab did not print.
            Assert.Equal(8, tools.Count);
            // Entry 169 section 7.2: the icon, with the tool's key beneath it that Alt shows.
            Assert.All(tools, t => Assert.IsType<PathIcon>(Assert.IsType<StackPanel>(t.Content).Children[0]));
            Assert.Equal(["Pan, and click a mark to select it (C or P)", "Scale: length (L)", "Scale: rectangle (R)", "Point of aim (A)", "Impact (I)", "Select (V)", "Place bulls, on a target GroupLab did not print (B)", "Lasso shots onto a bull (O)"], tools.Select(t => ToolTip.GetTip(t) as string));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
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
            // Entry 165 section 9 adds "Sending targets", its own section rather than a toggle among the others.
            // Entry 208 section 4 gathers sending targets, error reports and the survey under one section, "Sharing".
            Assert.Equal(["Units", "Theme", "This build", "Updates", "Sharing", "Diagnostics", "Crash records"], settings);
            window.ShowSettings(false);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
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
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            // Entry 111 section 3: each "why" is a small button beside its item, and what it opens is hidden until it is opened.
            var bodies = window.GetLogicalDescendants().OfType<StackPanel>().Where(b => b.Classes.Contains(AppStyles.WhyBody) && Shown((Control)b.Parent!)).ToList();
            Assert.NotEmpty(bodies);
            Assert.All(bodies, b => Assert.False(b.IsVisible));

            var shape = window.JudgementCards[0];
            // Entry 163 section 5 changed entry 111's card: it opens as its verdict alone, and the evidence is behind the verdict's "why"
            // rather than in view. The first real user found the analysis screen explaining too much before anybody asked.
            var shown = window.GetLogicalDescendants().OfType<Border>().Single(b => b.Name == "shapeCard").GetLogicalDescendants().OfType<TextBlock>()
                .Where(t => !t.GetLogicalAncestors().OfType<StackPanel>().Any(a => a.Classes.Contains(AppStyles.WhyBody)) && !t.GetLogicalAncestors().OfType<Button>().Any())
                .Select(t => t.Text ?? "").ToList();
            Assert.Single(shown);
            Assert.StartsWith("Circularity test, ", shape[1], StringComparison.Ordinal);
            Assert.Contains(": p = ", shape[1], StringComparison.Ordinal);
            Assert.StartsWith("Vertical stringing", shape[2], StringComparison.Ordinal);
            Assert.Contains(shape, l => l.StartsWith("Error ellipse", StringComparison.Ordinal));

            var zero = window.ZeroText.ToList();
            Assert.Contains(zero, t => t.StartsWith("Not distinguishable from zero at ", StringComparison.Ordinal) || t.StartsWith("Dial", StringComparison.Ordinal));

            // One number per shot: shots are named by their bull, so no bull column when every shot sits on its own.
            Assert.DoesNotContain(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "bull" && Shown(t));
            Assert.Contains(window.Plot.Legend, l => l.Contains("a dot at each center", StringComparison.Ordinal));
            window.Plot.ShowOutlines = false;
            Assert.Contains(window.Plot.Legend, l => l.Contains("outlines are hidden", StringComparison.Ordinal));

            window.SetEveryWhy(true);
            Assert.All(window.GetLogicalDescendants().OfType<StackPanel>().Where(b => b.Classes.Contains(AppStyles.WhyBody)), b => Assert.True(b.IsVisible));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
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
            // Entry 131 section 1.2 asks for these two sizes: the smallest window a person is likely to use, and a high resolution one where
            // loose spacing and mixed type sizes show up rather than hiding in a crowd.
            // 1400 by 900 is the size the website shows, and it had fallen out of this walk: the site was serving screenshots from
            // 2026-09-19 of an interface that had changed every day since. Entry 144 section 4 is the job that keeps them current; this is
            // the size it needs to exist for. 1920 by 1080 is the Store listing's size (docs/store/LISTING.md), and it had fallen out too,
            // leaving the listing with the screens as they were at entry 112; entry 247 put it back.
            foreach (var (width, height) in new[] { (1280, 720), (1400, 900), (1920, 1080), (2560, 1440) })
            {
                var (window, path, synthetic) = Sheet(width, height);
                long[]? suppressor = null;
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

                    // Entry 243 section 1.3, answering question 60: the published Compare loads picture is Alan's own two suppressor sheets,
                    // under entry 171's standing consent, each analyzed as a person would and compared. Publishing refuses to go on without
                    // them, so SOURCES.md is always true of what it lists; an ordinary test run without them compares the synthetic sheet.
                    if (suppressor is null && SuppressorScans() is { } scans)
                    {
                        suppressor = [AnalyzeScan(window, scans.Dominus, "6 ARC, Dominus K"), AnalyzeScan(window, scans.Magnus, "6 ARC, Magnus S")];
                        window.OpenImage(path);
                        window.ApplyDetection(synthetic);
                        window.Session.SetCalibre(Calibre.Of(0.308));
                        window.Session.SetShotDistance(3600);
                        window.CalibreAnswered();
                        window.Session.SetEquipment(rifle, null, "Test load");
                    }

                    foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                    {
                        string size = $"{width}x{height}";
                        window.SetTheme(theme);
                        window.BackToEditor();
                        window.Canvas.FitToView();
                        Save(window, $"marking-{name}-{size}");
                        window.CalibreAnswered();
                        window.Analyse();
                        window.SetEveryWhy(false);
                        Save(window, $"analysis-{name}-{size}");
                        window.SetEveryWhy(true);
                        Save(window, $"analysis-open-{name}-{size}");
                        window.SetEveryWhy(null);

                        // Entry 253 section 2: Shots Needed to Zero open, worked out from the test rifle's quarter-MOA clicks.
                        window.AdvancedPanel.IsExpanded = true;
                        window.FullFiguresPanel.IsExpanded = true;
                        Dispatcher.UIThread.RunJobs();
                        Assert.True(window.ShotsToZeroWork.Wait(TimeSpan.FromSeconds(60)), "Shots Needed to Zero did not finish");
                        Dispatcher.UIThread.RunJobs();
                        window.BringShotsToZeroIntoView();
                        Save(window, $"shots-to-zero-{name}-{size}");
                        window.FullFiguresPanel.IsExpanded = false;
                        window.AdvancedPanel.IsExpanded = false;
                        window.ShowSettings();
                        Save(window, $"settings-{name}-{size}");
                        window.ShowSettings(false);
                        window.ShowSessions();
                        Save(window, $"sessions-{name}-{size}");
                        window.ShowSessions(false);
                        // Entry 155: the library and the print screen are one screen, Targets, photographed once with a sheet chosen.
                        window.ShowLibrary();
                        window.ChooseLibrarySheet(window.TargetsPanel.Sheets.First(s => s.File == "GL-CF25-LTR.gltd.json").Definition.Name);
                        Save(window, $"targets-{name}-{size}");

                        // Entry 253 section 2: a C3 zeroing grid in the preview, its words drawn.
                        window.ChooseLibrarySheet(window.TargetsPanel.Sheets.First(s => s.File == "GL-ZERO-MOA-100Y.gltd.json").Definition.Name);
                        Save(window, $"targets-zero-{name}-{size}");

                        // Entry 242 section 1: "Made for your optic" filled in, for its tour stop: 100 yd through 10x, and through 4x, the set.
                        var ten = window.TargetsPanel.Generate("100", "10", "", 25)!;
                        Save(window, $"optic-{name}-{size}");
                        var four = window.TargetsPanel.Generate("100", "4", "", 25)!;
                        Save(window, $"optic-4x-{name}-{size}");
                        window.TargetsPanel.ShowDesigner(false);

                        // The tour stop's numbers, from the generator itself rather than typed (entry 242 section 1): the site reads this file.
                        static object Numbers(GroupLab.Cli.Library.GeneratedTargets made) => new
                        {
                            said = made.Explanation,
                            centerInches = Math.Round(GroupLab.Cli.Library.TargetGenerator.ArcminutesSeen / made.Request.LowestMagnification * 1.0472 * made.Request.DistanceYards / 100, 2, MidpointRounding.AwayFromZero),
                            discInches = Math.Round(made.OuterDmm / 254.0, 2),
                            pitchInches = Math.Round(made.PitchInches, 2),
                            bullsPerSheet = made.Columns * made.Rows,
                            sheets = made.Sheets,
                        };
                        string numbers = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            arcminutes = GroupLab.Cli.Library.TargetGenerator.ArcminutesSeen,
                            dotMargin = GroupLab.Cli.Library.TargetGenerator.DotMargin,
                            at10x = Numbers(ten),
                            at4x = Numbers(four),
                        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        foreach (string output in outputs)
                        {
                            File.WriteAllText(Path.Combine(output, "optic-numbers.json"), numbers + "\n");
                        }
                        window.ShowLibrary(false);
                        window.ShowBallistics();
                        window.ProjectGroup("600", 0, "4", "4", "2");
                        Save(window, $"ballistics-{name}-{size}");
                        // Entry 253 section 2: the other view, the chance of a hit on a 30 in square at 600 yd.
                        window.ShowBallisticView(MainWindow.BallisticView.Hit);
                        window.WorkOutHit("600", 0, "30", "30");
                        Save(window, $"ballistics-hit-{name}-{size}");
                        window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
                        window.ShowBallistics(false);

                        // Entry 131 section 7: the Equipment screen, with a rifle and a load on it so the lists are not empty.
                        window.ShowEquipment(GroupLab.Core.Marking.EquipmentKind.Rifle);
                        Save(window, $"equipment-{name}-{size}");
                        window.BackToEditor();

                        Assert.False(suppressor is null && outputs.Count > 1, "publishing the screenshots needs Alan's two 6 ARC scans from the test-data release (GROUPLAB_TEST_DATA)");
                        if (suppressor is { } pair)
                        {
                            window.ShowSessions();
                            window.ChooseSession(pair[0], true);
                            window.ChooseSession(pair[1], true);
                        }
                        else if (window.Sessions!.List().Count < 2)
                        {
                            // Entry 113 section 2: two sessions of the sheet compared, the second saved as another load the first time round.
                            long saved = window.CurrentSession!.Value;
                            long copy = window.Sessions.Save(window.Sessions.Get(saved)! with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", ShotDate = "2099-01-01", Load = "Second load" });
                            window.ChooseSession(saved, true);
                            window.ChooseSession(copy, true);
                        }

                        window.CompareChosen();
                        Save(window, $"compare-{name}-{size}");
                        window.ShowCompare(false);
                    }

                    window.Close();
                }
                finally
                {
                    GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
                }

                // Entry 253 section 2: the first run, as a new install opens, with the benchmark offered.
                foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                {
                    TestDefaults.Outside.Forget();
                    string fresh = Path.Combine(Path.GetTempPath(), $"grouplab-entry253-{Guid.NewGuid():N}");
                    var first = new MainWindow(new AppSettingsStore(Path.Combine(fresh, "settings.json")))
                    {
                        Width = width, Height = height, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true,
                    };
                    try
                    {
                        first.Show();
                        first.SetTheme(theme);
                        first.ShowFirstRunIfDue();
                        Assert.True(first.FirstRunShown);
                        Save(first, $"firstrun-{name}-{width}x{height}");
                    }
                    finally
                    {
                        first.Close();
                        TestDefaults.Outside.Forget();
                        GroupLab.Tests.Support.Temp.Delete(fresh);
                    }
                }
            }
        }
        finally
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
        }

        SheetPictures(outputs);
    }

    /// <summary>
    /// Entry 256: a Features entry that names a bull, a grid or a sheet shows that thing itself, drawn from the library as it prints, cropped
    /// to it with a little margin, on its own white paper so one picture reads in both themes. The grids keep their legend and the numbers
    /// outside them, so the scale can be read.
    /// </summary>
    private static void SheetPictures(IReadOnlyList<string> outputs)
    {
        const double dpi = 200;
        const double perDmm = dpi / 254;

        static GroupLab.Core.Gltd.Model.TargetDefinition Load(string file) =>
            GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", file)).Definition!;

        GrayImage Draw(GroupLab.Core.Gltd.Model.TargetDefinition d, int page, double left, double top, double right, double bottom)
        {
            var scene = SceneBuilder.Build(d).Pages[page];
            left = Math.Max(0, left);
            top = Math.Max(0, top);
            right = Math.Min(scene.Width / 2.0, right);
            bottom = Math.Min(scene.Height / 2.0, bottom);
            var region = new PixelRegion((int)Math.Round(left * perDmm), (int)Math.Round(top * perDmm), (int)Math.Round((right - left) * perDmm), (int)Math.Round((bottom - top) * perDmm));
            return SceneRasterizer.Rasterize(scene, dpi, 1.0, region, words: true);
        }

        void Save(string name, GrayImage image)
        {
            using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
            foreach (string output in outputs)
            {
                Cv2.ImWrite(Path.Combine(output, $"sheet-{name}.png"), mat);
            }
        }

        // One bull, a little more than its outer disc on every side.
        foreach (var (name, file) in new[] { ("e-bull", "GL-CF25-LTR-E.gltd.json"), ("c-bull", "GL-CF25-LTR-C.gltd.json") })
        {
            var d = Load(file);
            var bull = d.Bulls.First(b => b.Scoring);
            double reach = 0.6 * d.RingSets.First(r => r.Key == bull.RingSet).Discs[0].Diameter;
            Save(name, Draw(d, 0, bull.X - reach, bull.Y - reach, bull.X + reach, bull.Y + reach));
        }

        // Each zeroing grid, from the legend at the top to the numbers under it, and the numbers each side.
        foreach (string file in new[] { "GL-ZERO-MOA-100Y", "GL-ZERO-MIL-100Y", "GL-ZERO-MOA-100M", "GL-ZERO-MIL-100M" })
        {
            var d = Load(file + ".gltd.json");
            var g = d.Grids![0];
            double halfX = g.FieldX ?? g.Half, halfY = g.FieldY ?? g.Half;
            Save(file.Replace("GL-", "", StringComparison.Ordinal).ToLowerInvariant(),
                Draw(d, 0, g.CentreX - halfX - 130, 40, g.CentreX + halfX + 130, g.CentreY + halfY + 110));
        }

        // Entry 264: every donor sheet whole, small, for the Shoot a target page's picture of the sheet itself.
        var donor = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(Repository(), "website", "donor", "sheets.json")))!["sheets"]!.AsArray();
        foreach (string file in donor.Select(s => (string)s!["letter"]!))
        {
            var d = Load(file + ".gltd.json");
            Save("page-" + file.Replace("GL-", "", StringComparison.Ordinal).ToLowerInvariant(), SceneRasterizer.Rasterize(SceneBuilder.Build(d).Pages[0], 40, words: true));
        }

        // A large format set: its four Letter sheets, whole, side by side as they are laid out to shoot.
        {
            var d = Load("GL-LR25-T.gltd.json");
            var pages = SceneBuilder.Build(d).Pages;
            const double small = 40;
            var drawn = pages.Select(p => SceneRasterizer.Rasterize(p, small, words: true)).ToList();
            int cols = d.Tiling!.Cols, rows = d.Tiling.Rows, gap = 12, w = drawn[0].Width, h = drawn[0].Height;
            var all = new byte[((cols * w) + ((cols - 1) * gap)) * ((rows * h) + ((rows - 1) * gap))];
            Array.Fill(all, (byte)200);
            int width = (cols * w) + ((cols - 1) * gap);
            for (int i = 0; i < drawn.Count; i++)
            {
                int x0 = (i % cols) * (w + gap), y0 = (i / cols) * (h + gap);
                for (int y = 0; y < h; y++)
                {
                    Array.Copy(drawn[i].Pixels, y * w, all, ((y0 + y) * width) + x0, w);
                }
            }

            Save("large-set", new GrayImage(width, (rows * h) + ((rows - 1) * gap), all));
        }
    }
}
