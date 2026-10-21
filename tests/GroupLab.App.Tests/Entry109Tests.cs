using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    internal static (string Dominus, string Magnus)? SuppressorScans()
    {
        string? folder = Environment.GetEnvironmentVariable("GROUPLAB_TEST_DATA");
        string dominus = Path.Combine(folder ?? "", "load-sheet-6arc-dominus-k-2026-09-26.png");
        string magnus = Path.Combine(folder ?? "", "load-sheet-6arc-magnus-m-2026-09-26.png");
        return folder is { Length: > 0 } && File.Exists(dominus) && File.Exists(magnus) ? (dominus, magnus) : null;
    }

    /// <summary>One scan opened, detected, analyzed and saved as a session under <paramref name="load"/>; the session's id.</summary>
    internal static long AnalyzeScan(MainWindow window, string scan, string load)
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
                [$"New target ({CommandKey.Label("N")})", "Open image…", $"Paste an image ({CommandKey.Label("V")})", "Open marking…", "Export…", "Share a session file…", "Open a session file…", "Import shots from a CSV…", "Open the session's folder", "Report a problem…", "Walk me through a demo"],
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
            // Entry 298 adds "Layout", with Reset layout.
            // Entry 314 adds "Caliber box shows", what the caliber box offers.
            // Entry 463 adds "Sync between devices", after Saving.
            Assert.Equal(["Units", "Zero correction", "Caliber box shows", "Printers", "Saving", "Sync between devices", "Theme", "Layout", "This build", "Updates", "Sharing", "Your data", "Diagnostics", "Crash records"], settings);
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
            // Entry 437: a notice fades by itself a few seconds after it appears, so no picture shows one. The walk analyzes the same
            // sample many times, and its "another copy of a sheet" notices had piled up over the corner of every later screen, covering
            // controls the guide's markings point at.
            if (window is MainWindow main)
            {
                main.Confirmations.Clear();
            }

            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);

            // Entry 462 section 3: with GROUPLAB_LAYOUT_AUDIT naming a file, every screen photographed is also checked for words cut off,
            // controls past the window's edge, words over words and contrast, at every size and in both themes, one line a finding.
            if (Environment.GetEnvironmentVariable("GROUPLAB_LAYOUT_AUDIT") is { Length: > 0 } audit)
            {
                File.AppendAllLines(audit, GroupLab.Tests.Support.LayoutAudit.Findings(window).Select(f => name + "\t" + f));
            }
            foreach (string output in outputs)
            {
                frame.Save(Path.Combine(output, name + ".png"), new PngBitmapEncoderOptions());
                // Entry 437 section 2.1: the size the site shows gets its controls' positions; both themes lay out the same, so once.
                const string Shown = "-light-1400x900";
                if (name.EndsWith(Shown, StringComparison.Ordinal))
                {
                    File.WriteAllText(Path.Combine(output, name[..^Shown.Length] + "-bounds.json"), ScreenBounds.Json(window, frame.PixelSize.Width, frame.PixelSize.Height));
                }
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
                    // Entry 294 section 2: its scope is in mil, a tenth a click, so the zero, the dope and Shots Needed to Zero are pictured
                    // in mil while Settings says MOA; Compare and Settings stay in MOA, so each unit is shown.
                    // Entry 456: a scope and mount and a weight, for the rifle page's Scope and mount picture; its scale factor stays 1, so no
                    // other picture's clicks change.
                    var rifle = new Rifle("Test rifle", 0.1, GroupLab.Core.Statistics.AngularUnit.Mrad)
                    {
                        SightHeightInches = 1.75, ZeroDistanceYards = 100, ScopeTravel = 30, ScopeTravelKind = GroupLab.Core.Ballistics.ScopeTravelKind.Total, RailCant = 6,
                        WeightAsFiredLb = 11,
                    };
                    // Entry 407 section 5: the rifle with two barrels, one of them on now, so Equipment shows a rifle's page with its barrels.
                    window.Book = RecordBook.Empty.With(rifle).With(new Load("Test load", null)
                    {
                        MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 10, BallisticCoefficient = 0.326, DragModel = GroupLab.Core.Ballistics.DragModel.G7,
                        BcReference = GroupLab.Core.Ballistics.ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
                    }).With(new Barrel("Bartlein", "Test rifle", 412) { LengthInches = 26, TwistInches = 8, TwistDirection = 1 })
                        .With(new Barrel("Factory", "Test rifle", 1180) { LengthInches = 24, TwistInches = 8, TwistDirection = 1 })
                        .PutOn("Test rifle", "Bartlein");
                    rifle = window.Book.FindRifle("Test rifle")!;
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

                        // Entry 407 sections 1 to 3, each view's own picture for the Features page (entry 256): a bull chosen with its lines
                        // and the panel's block; the Groups section with two bulls that took two; a shot open in the Shots list.
                        // Bull 1 chosen and given bull 2's hole, so its blue line reaches across, zoomed in on the two; undone after.
                        var scoringBulls = window.Session.State.Bulls.Where(b => b.Scoring).ToList();
                        var lent = window.Session.State.Shots.First(s => s.IsShot && s.Bull == scoringBulls[1].Index);
                        window.Canvas.Pick(scoringBulls[0].Index);
                        BullChoosing.Tap(window.Session, scoringBulls[0].Index, lent.Id);
                        window.Canvas.ZoomBy(2.5);
                        window.Canvas.CentreOn(new PointD((scoringBulls[0].Image.X + scoringBulls[1].Image.X) / 2, scoringBulls[0].Image.Y));
                        Dispatcher.UIThread.RunJobs();
                        window.BringBullLinesIntoView();
                        Save(window, $"bull-lines-{name}-{size}");
                        window.Session.Undo();
                        window.Canvas.Pick(null);
                        window.Canvas.FitToView();
                        window.ChooseShotPattern(ShotPattern.SomeTookMore);
                        foreach (var two in window.Session.State.Bulls.Where(b => b.Scoring).Skip(2).Take(2).ToList())
                        {
                            window.TapBullForGroups(two.Index);
                        }

                        Dispatcher.UIThread.RunJobs();
                        window.BringGroupsIntoView();
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"groups-{name}-{size}");
                        window.ChooseShotPattern(ShotPattern.OnePerBull);
                        window.Canvas.Selected = window.Session.State.Shots.First(s => s.IsShot).Id;
                        window.RefreshForTests();
                        Dispatcher.UIThread.RunJobs();
                        window.BringShotsIntoView();
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"shots-open-{name}-{size}");
                        window.Canvas.Selected = null;
                        window.RefreshForTests();

                        // Entry 407 section 6: the paper and backing fold opened, at the bottom of the setup.
                        window.MaterialFoldOpen = true;
                        Dispatcher.UIThread.RunJobs();
                        window.BringMaterialIntoView();
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"paper-fold-{name}-{size}");
                        window.MaterialFoldOpen = false;

                        // Entry 274, answering question 66: a target GroupLab did not print, marked by hand as a person would: a plain sample
                        // drawn here, with no markers or codes, its scale set from a ring's known width, its bulls and shots placed by hand.
                        string other = PlainSample();
                        try
                        {
                            window.OpenImage(other);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.CalibreAnswered();
                            Dispatcher.UIThread.RunJobs();
                            window.Session.SetScale(new LengthReference(new PointD(PlainCentres[0].X - PlainOuter, PlainCentres[0].Y), new PointD(PlainCentres[0].X + PlainOuter, PlainCentres[0].Y), 2.0 * PlainOuter / PlainDpi));
                            foreach (var centre in PlainCentres)
                            {
                                window.Session.AddBull(centre);
                            }

                            foreach (var (x, y) in PlainHoles)
                            {
                                window.Session.AddShot(new PointD(x, y));
                            }

                            window.Canvas.FitToView();
                            Save(window, $"marking-other-{name}-{size}");
                        }
                        finally
                        {
                            window.OpenImage(path);
                            window.ApplyDetection(synthetic);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.Session.SetEquipment(rifle, null, "Test load");
                            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(other)!);
                        }

                        // Entries 340 and 341, entry 256's own picture: a store-bought target recognized, its bull placed and the scale
                        // warning beside it, with the window asking which size over the picture. The target is a stand-in GroupLab draws,
                        // because a maker's artwork is never published (entry 340); recognition is given the numbers a crop of the 8 in
                        // bullseye measured, so the window asks.
                        string bought = StoreTargetStandIn();
                        try
                        {
                            window.OpenImage(bought);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.CalibreAnswered();
                            Dispatcher.UIThread.RunJobs();
                            var eight = GroupLab.Core.StoreTargets.StoreTargetLibrary.Find("bc-34805-shoot-n-c-8in-bull")!;
                            var six = GroupLab.Core.StoreTargets.StoreTargetLibrary.Find("bc-34550-shoot-n-c-6in-bull")!;
                            GroupLab.Core.StoreTargets.StoreTargetCandidate Fit(GroupLab.Core.StoreTargets.StoreTarget t, double ppi, int features, double layout)
                            {
                                var bull = t.Fingerprint.Bulls[0];
                                return new(t, features, new Homography([ppi, 0, StandInBull - (ppi * bull.X), 0, ppi, StandInBull - (ppi * bull.Y), 0, 0, 1]), layout);
                            }

                            var seen = GroupLab.Core.StoreTargets.StoreTargetRecognizer.Decide([Fit(eight, StandInDpi, 200, 0.988), Fit(six, StandInDpi * 8 / 6, 72, 0.928)]);
                            var asked = window.ApplyRecognition(seen);
                            Dispatcher.UIThread.RunJobs();
                            var question = window.WhichTargetWindow!;
                            Dispatcher.UIThread.RunJobs();
                            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                            Dispatcher.UIThread.RunJobs();
                            using var asking = question.CaptureRenderedFrame()!;
                            string dialogFile = Path.Combine(Path.GetDirectoryName(bought)!, "question.png");
                            asking.Save(dialogFile, new PngBitmapEncoderOptions());
                            question.GetLogicalDescendants().OfType<Button>()
                                .First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Shoot-N-C 8 in bullseye")
                                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                            Dispatcher.UIThread.RunJobs();
                            asked.GetAwaiter().GetResult();
                            window.Canvas.FitToView();
                            Dispatcher.UIThread.RunJobs();
                            window.ScaleInputs.BringIntoView();
                            Dispatcher.UIThread.RunJobs();
                            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                            Dispatcher.UIThread.RunJobs();
                            using var behind = window.CaptureRenderedFrame()!;
                            string behindFile = Path.Combine(Path.GetDirectoryName(bought)!, "behind.png");
                            behind.Save(behindFile, new PngBitmapEncoderOptions());

                            // The question laid over the picture's lower left, where it covers neither the bull nor the scale and its warning.
                            using var whole = Cv2.ImRead(behindFile, ImreadModes.Unchanged);
                            using var over = Cv2.ImRead(dialogFile, ImreadModes.Unchanged);
                            int x = (int)(whole.Width * 0.04), y = Math.Max(0, whole.Height - over.Height - (int)(whole.Height * 0.06));
                            var room = new OpenCvSharp.Rect(x, y, Math.Min(over.Width, whole.Width - x), Math.Min(over.Height, whole.Height - y));
                            using (var into = new Mat(whole, room))
                            using (var part = new Mat(over, new OpenCvSharp.Rect(0, 0, room.Width, room.Height)))
                            {
                                part.CopyTo(into);
                            }

                            Cv2.Rectangle(whole, room, Scalar.All(128), 1);
                            foreach (string output in outputs)
                            {
                                Cv2.ImWrite(Path.Combine(output, $"store-target-{name}-{size}.png"), whole);
                            }
                        }
                        finally
                        {
                            window.OpenImage(path);
                            window.ApplyDetection(synthetic);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.Session.SetEquipment(rifle, null, "Test load");
                            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(bought)!);
                        }

                        // Entry 356, board B: a GroupLab sheet whose corner codes would not read, said in the middle of the window, and
                        // board "Not an error", the calm question for a target GroupLab does not know.
                        string codeless = CodelessSheet();
                        try
                        {
                            window.OpenImage(codeless);
                            Dispatcher.UIThread.RunJobs();
                            window.ShowSheetProblemForScreens(GroupLab.Core.Gltd.Binary.GltdBinary.Encode(GltdJsonReader.ReadFile(Path.Combine(AppContext.BaseDirectory, "targets", "GL-RF25-LTR.gltd.json")).Definition!).Encoding!.DefinitionId);
                            window.Canvas.FitToView();
                            Save(window, $"problem-dialog-{name}-{size}");
                            window.DismissProblem();
                            Save(window, $"problem-dismissed-{name}-{size}");
                            window.ShowWhichTargetForScreens();
                            Save(window, $"which-target-{name}-{size}");
                            window.DismissProblem();
                        }
                        finally
                        {
                            window.OpenImage(path);
                            window.ApplyDetection(synthetic);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.Session.SetEquipment(rifle, null, "Test load");
                            File.Delete(codeless);
                        }

                        window.CalibreAnswered();
                        window.Analyse();
                        // Entry 425: the analysis is pictured with a shot picked, one end of the extreme spread, on the red line as in the report.
                        if (window.Plot.SpreadPair is { } widest)
                        {
                            window.PickShots([widest.First]);
                        }

                        window.SetEveryWhy(false);
                        Save(window, $"analysis-{name}-{size}");
                        window.SetEveryWhy(true);
                        Save(window, $"analysis-open-{name}-{size}");
                        window.PickShots([]);
                        window.SetEveryWhy(null);

                        // Entry 442: the shot table worked from the keyboard, a row chosen with Space and the focus ring kept on it.
                        var keyRow = window.OffsetRows[Math.Min(4, window.OffsetRows.Count - 1)];
                        keyRow.Focus(Avalonia.Input.NavigationMethod.Tab);
                        window.KeyPress(Avalonia.Input.Key.Space, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Space, " ");
                        window.KeyRelease(Avalonia.Input.Key.Space, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Space, " ");
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"keyboard-{name}-{size}");
                        window.PickShots([]);
                        window.Focus();

                        // Entry 462 section 4: the group tools' own windows for the Features page (entry 256), each at the walk's size: the
                        // Shots window with its first shot left out, Zero from this group, and Share a picture with its results box.
                        int leftOut = window.Session.State.Shots.First(s => s.IsShot).Id;
                        window.Session.SetExclusion(leftOut, ExclusionReason.ByShooter);
                        Dispatcher.UIThread.RunJobs();
                        foreach (var (tool, toolName) in new (Func<Avalonia.Controls.Window> Make, string Name)[] { (window.ShotsWindow, "shots-window"), (window.ZeroFromWindow, "zero-window"), (window.ShareWindow, "share-window") })
                        {
                            var opened = tool();
                            opened.Width = width;
                            opened.Height = height;
                            opened.Show(window);
                            Dispatcher.UIThread.RunJobs();
                            Save(opened, $"{toolName}-{name}-{size}");
                            opened.Close();
                        }

                        window.Session.SetExclusion(leftOut, null);
                        Dispatcher.UIThread.RunJobs();

                        // Entry 462 section 4: a word's explanation open where it appears, as the keyboard opens it (TermHelp).
                        var term = window.GetVisualDescendants().OfType<TextBlock>().First(t => t.Classes.Contains(TermHelp.Class) && t.Text == "Mean radius" && t.IsEffectivelyVisible);
                        Avalonia.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(term);
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"word-explained-{name}-{size}");
                        Avalonia.Controls.Primitives.FlyoutBase.GetAttachedFlyout(term)?.Hide();
                        Dispatcher.UIThread.RunJobs();

                        // Entry 407 section 7: no readings yet, and the speeds typed on the analysis itself.
                        window.OpenReadingsHere("2705 2711 2698 2716 2702");
                        window.BringVelocityIntoView();
                        Save(window, $"readings-here-{name}-{size}");
                        window.CloseReadingsHere();

                        // Entry 253 section 2: Shots Needed to Zero open, worked out from the test rifle's tenth-mil clicks.
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
                        // Entry 437 section 1.3: the lower half of Settings, from Your data down, which the user guide's section 12 describes.
                        var yourData = window.GetLogicalDescendants().OfType<TextBlock>().First(t => t.Text == "Your data");
                        yourData.BringIntoView(new Avalonia.Rect(0, 0, 1, 4000));
                        Save(window, $"settings-data-{name}-{size}");

                        // Entry 463: Sync between devices in Settings, a folder chosen and synced with another device in it (1A, with the
                        // cards of 3B), and a session changed on both laid out side by side (2A). The other device is a second store the walk
                        // makes, named "Range laptop"; the choice is settled after each theme's pictures, so no later picture shows a banner.
                        string syncRoot = Path.Combine(Path.GetDirectoryName(path)!, $"sync-walk-{size}");
                        string cloudFolder = Path.Combine(syncRoot, "OneDrive", "GroupLab");
                        Directory.CreateDirectory(cloudFolder);
                        var laptop = GroupLab.Core.Records.SessionStore.Open(Path.Combine(syncRoot, "laptop.db"));
                        var laptopLocal = new GroupLab.Core.Sync.FileSyncLocal(Path.Combine(syncRoot, "sheets"), Path.Combine(syncRoot, "settings.json"));
                        var cloud = new GroupLab.Core.Sync.DirectorySyncFolder(cloudFolder);
                        void Synced(Task run)
                        {
                            var until = DateTime.UtcNow.AddMinutes(1);
                            while (!run.IsCompleted && DateTime.UtcNow < until)
                            {
                                Dispatcher.UIThread.RunJobs();
                                Thread.Sleep(5);
                            }

                            Dispatcher.UIThread.RunJobs();
                        }

                        if (window.SessionsToChoose().Count == 0)
                        {
                            window.NameSyncDevice("Desk computer");
                            window.ChooseSyncFolder(cloudFolder);
                            Synced(window.SyncNowAsync());
                            GroupLab.Core.Sync.SyncEngine.Run(laptop, laptopLocal, cloud, "Range laptop", DateTime.UtcNow);
                            var shared = window.Sessions!.List().First();
                            window.Sessions.Save(window.Sessions.Get(shared.Id)! with { Load = name == "dark" ? "Test load, 41.5 gr" : "Test load, 41.7 gr" });
                            var there = laptop.List().First(s => s.SheetName == shared.SheetName && s.CreatedUtc == shared.CreatedUtc);
                            laptop.Save(laptop.Get(there.Id)! with { Barrel = "Bartlein", MeanRadiusInches = (there.MeanRadiusInches ?? 0.1) * 1.08 });
                            Synced(window.SyncNowAsync());
                            GroupLab.Core.Sync.SyncEngine.Run(laptop, laptopLocal, cloud, "Range laptop", DateTime.UtcNow);
                            Synced(window.SyncNowAsync());
                        }

                        window.FillSync();
                        Dispatcher.UIThread.RunJobs();
                        var syncHeading = window.GetLogicalDescendants().OfType<TextBlock>().First(t => t.Text == GroupLab.Core.Sync.SyncScreens.Title);
                        // The section from its heading down: the scroller is set so the heading is at the top of the view.
                        if (Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<ScrollViewer>(syncHeading) is { Content: Avalonia.Visual inside } scroller
                            && syncHeading.TranslatePoint(new Avalonia.Point(0, 0), inside) is { } at)
                        {
                            scroller.Offset = new Avalonia.Vector(0, Math.Max(0, at.Y - 8));
                        }

                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"sync-settings-{name}-{size}");
                        window.ShowSessions();
                        window.ShowSyncConflict(window.SessionsToChoose()[0].Conflict.Id);
                        Dispatcher.UIThread.RunJobs();
                        Save(window, $"sync-conflict-{name}-{size}");
                        window.PressProblemChoice(GroupLab.Core.Sync.SyncScreens.KeepThis(window.Sync.DeviceName));
                        window.StopSyncing();
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                        window.ShowSessions(false);
                        window.ShowSettings();
                        Dispatcher.UIThread.RunJobs();

                        // The other theme is photographed in this same window, and its Settings picture starts from the top.
                        Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<ScrollViewer>(yourData)?.ScrollToHome();
                        window.ShowSettings(false);
                        window.ShowSessions();
                        Save(window, $"sessions-{name}-{size}");
                        window.ShowSessions(false);
                        // Entry 155: the library and the print screen are one screen, Targets, photographed once with a sheet chosen.
                        window.ShowLibrary();
                        window.ChooseLibrarySheet(window.TargetsPanel.Sheets.First(s => s.File == "GL-CF25-LTR.gltd.json").Definition.Name);
                        Save(window, $"targets-{name}-{size}");

                        // Entry 253 section 2: a C3 zeroing grid in the preview, its words drawn.
                        window.ChooseLibrarySheet(window.TargetsPanel.Sheets.First(s => s.File == "GL-ZERO-MIL-100Y.gltd.json").Definition.Name);
                        Save(window, $"targets-zero-{name}-{size}");

                        // Entry 348: Add a store-bought target at its second step, a stand-in poster drawn by GroupLab, its corners found and its
                        // printed size typed, as the Features page and the tour show it.
                        string posterFolder = GroupLab.Tests.Support.Temp.Folder("fingerprint-walk");
                        Directory.CreateDirectory(posterFolder);
                        try
                        {
                            string poster = Path.Combine(posterFolder, "poster.jpg");
                            GroupLab.Tests.Support.StandInPoster.Photograph(poster);
                            var fingerprint = window.AddStoreTarget();
                            fingerprint.UsePhoto(poster);
                            fingerprint.GoOn();
                            Assert.True(fingerprint.Work.Wait(TimeSpan.FromSeconds(60)));
                            Dispatcher.UIThread.RunJobs();
                            fingerprint.FieldNamed("Width in inches").Text = "12";
                            fingerprint.FieldNamed("Height in inches").Text = "18";
                            window.Confirmations.Clear();
                            Save(window, $"fingerprint-{name}-{size}");
                            fingerprint.GoBack();
                            fingerprint.GoBack();
                        }
                        finally
                        {
                            GroupLab.Tests.Support.Temp.Delete(posterFolder);
                        }

                        window.ShowLibrary();

                        // Entry 365: Targets with Scale markers open, the Features page's picture of the four markers.
                        window.ShowScaleMarkers(true);
                        Save(window, $"scale-markers-{name}-{size}");
                        window.ShowScaleMarkers(false);

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
                        // Entry 455: the third view, point blank range for a whitetail at 9 in 10 from a 0.63 MOA mean radius.
                        window.ShowBallisticView(MainWindow.BallisticView.PointBlank);
                        window.SetPointBlank("0.63", "90", "Whitetail deer");
                        window.FillPointBlank();
                        Save(window, $"ballistics-point-{name}-{size}");
                        window.ShowBallisticView(MainWindow.BallisticView.Trajectory);
                        window.ShowBallistics(false);

                        // Entry 131 section 7: the Equipment screen, with a rifle and a load on it so the lists are not empty. Entry 407 section
                        // 5: the rifle's own page, with its barrels and the one on now.
                        window.EditRifle("Test rifle");
                        Save(window, $"equipment-{name}-{size}");
                        // Entry 456: the same page at its Scope and mount part, with the figures, the tracking test and Top Gun.
                        window.ShowScopeAndMount();
                        Save(window, $"equipment-scope-{name}-{size}");
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
                            // Entry 462 section 3: a week before, not 2099, which the published pictures showed as the second load's date.
                            var weekBefore = DateTime.UtcNow.Date.AddDays(-7);
                            long copy = window.Sessions.Save(window.Sessions.Get(saved)! with
                            {
                                Id = 0, CreatedUtc = weekBefore.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture),
                                ShotDate = weekBefore.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Load = "Second load",
                            });
                            window.ChooseSession(saved, true);
                            window.ChooseSession(copy, true);
                        }

                        window.CompareChosen();
                        Save(window, $"compare-{name}-{size}");
                        window.ShowCompare(false);
                    }

                    // Entry 323: the Features page's own picture of "Velocity and the vertical", the block and the band, after every other
                    // screen so no other picture carries the readings. The synthetic sheet's group is a 100 yd group a tenth of an inch
                    // across, and readings that made velocity a share of it would spread 2 ft/s; so the plain sample target, marked by hand,
                    // with five shots on each bull spread as a good rifle groups at 300 yd, and readings made so that velocity is about a
                    // third of the vertical and follows the solver's slope, each paired with its shot.
                    string velocitySheet = PlainSample();
                    try
                    {
                        window.OpenImage(velocitySheet);
                        window.Session.SetCalibre(Calibre.Of(0.308));
                        window.Session.SetShotDistance(300 * 36);
                        window.Session.SetEquipment(rifle, null, "Test load");
                        window.CalibreAnswered();
                        Dispatcher.UIThread.RunJobs();
                        window.Session.SetScale(new LengthReference(new PointD(PlainCentres[0].X - PlainOuter, PlainCentres[0].Y), new PointD(PlainCentres[0].X + PlainOuter, PlainCentres[0].Y), 2.0 * PlainOuter / PlainDpi));
                        foreach (var centre in PlainCentres)
                        {
                            window.Session.AddBull(centre);
                        }

                        var spread = new Random(3231);
                        double Gauss() => Math.Sqrt(-2 * Math.Log(1 - spread.NextDouble())) * Math.Cos(2 * Math.PI * spread.NextDouble());
                        foreach (var centre in PlainCentres)
                        {
                            for (int shot = 0; shot < 5; shot++)
                            {
                                window.Session.AddShot(new PointD(centre.X + Math.Clamp(0.55 * PlainDpi * Gauss(), -190, 190), centre.Y + Math.Clamp(0.8 * PlainDpi * Gauss(), -250, 250)));
                            }
                        }

                        window.Analyse();
                        window.ShowBallistics();
                        window.ReadChronograph("Chronograph", "2026-10-01", string.Join(", ", Enumerable.Repeat(2710, 200)));
                        var order = window.ChronographPairing.Where(p => p.Shot is not null).Select(p => p.Shot!.Value).ToList();
                        var state = window.Session.State;
                        var byId = state.Shots.ToDictionary(s => s.Id);
                        var offsets = GroupAnalysis.CompositeOffsets(state, [.. order.Select(id => byId[id])]);
                        double[] up = [.. offsets.Select(o => -o.Y)];
                        double meanUp = up.Average(), sdUp = Math.Sqrt(up.Sum(u => (u - meanUp) * (u - meanUp)) / (up.Length - 1));
                        double k = GroupLab.Core.Ballistics.Projection.DropPerFps(new GroupLab.Core.Ballistics.BallisticInput(0.326, GroupLab.Core.Ballistics.DragModel.G7, 2710, 140, 1.75, 100), 300);
                        var noise = new Random(323);
                        double Normal() => Math.Sqrt(-2 * Math.Log(1 - noise.NextDouble())) * Math.Cos(2 * Math.PI * noise.NextDouble());
                        var readings = up.Select(u => Math.Round(2710 + (0.36 * (u - meanUp) / k) + (0.48 * sdUp / k * Normal()), 1));
                        window.ReadChronograph("Chronograph", "2026-10-01", string.Join(", ", readings.Select(r => r.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))));
                        window.AcceptChronograph();
                        window.ShowBallistics(false);
                        foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                        {
                            window.SetTheme(theme);
                            Dispatcher.UIThread.RunJobs();
                            Assert.True(window.Velocity is { HasResult: true }, window.Velocity?.Sentence);
                            window.BringVelocityIntoView();
                            Save(window, $"velocity-{name}-{width}x{height}");

                            // Entry 462 section 4: Conditions on the day open under the block (entry 416), for its own Features picture.
                            window.OpenConditions(true);
                            Dispatcher.UIThread.RunJobs();
                            window.BringVelocityIntoView();
                            Save(window, $"conditions-{name}-{width}x{height}");
                            window.OpenConditions(false);
                        }

                        // Entry 351: a timed string pairing, a row per reading, the fourth's choices open under its row: four readings of an
                        // earlier group, a six minute pause, then this group's, its first marked clean bore and its fifth left out.
                        int groupShots = window.Session.State.Shots.Count(s => s.IsShot);
                        var start = new TimeSpan(14, 2, 0);
                        var timed = Enumerable.Range(0, groupShots + 4).Select(i => new GroupLab.Core.Records.ChronographShot(i + 1, 2700.0 + ((i * 7) % 23) - 11,
                            CleanBore: i == 4, LeftOutByChronograph: i == 8, Time: start + TimeSpan.FromSeconds(30 * i) + (i >= 4 ? TimeSpan.FromMinutes(6) : TimeSpan.Zero))).ToList();
                        window.ShowBallistics();
                        window.ImportChronographStrings([new GroupLab.Core.Records.ChronographImport(GroupLab.Core.Records.ChronographFormat.GarminXero,
                            [.. timed.Select(t => t.Fps)], "A Garmin Xero export.", false, [], 1) { Name = "afternoon", Shots = timed }], "Garmin Xero");
                        window.ChangeReading(3);
                        foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                        {
                            window.SetTheme(theme);
                            Dispatcher.UIThread.RunJobs();
                            window.Confirmations.Clear();
                            window.BringChronographIntoView();
                            Save(window, $"pairing-{name}-{width}x{height}");
                        }

                        window.ClearChronograph();
                        window.ShowBallistics(false);
                    }
                    finally
                    {
                        GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(velocitySheet)!);
                    }

                    // Entry 423 section 4: the Features page's own picture of a set's tab, the three sheets of the 2 MOA set dropped together
                    // and pooled in one tab. Each sheet is given its detection through Check one sheet, as a person checking it would see.
                    string setFolder = Path.Combine(Path.GetTempPath(), $"grouplab-set-{Guid.NewGuid():N}");
                    Directory.CreateDirectory(setFolder);
                    try
                    {
                        var twoMoa = Entry423Tests.TwoMoaSet();
                        var sheets = new[] { 0, 1, 2 }.Select(k => Entry423Tests.DetectedSheet(twoMoa, setFolder, k)).ToList();
                        window.DetectOnOpen = false;
                        window.OpenDropped([.. sheets.Select(s => s.Path)]);
                        for (int i = 0; i < 2000 && !window.Dropping.IsCompleted; i++)
                        {
                            Dispatcher.UIThread.RunJobs();
                            System.Threading.Thread.Sleep(10);
                        }

                        Assert.True(window.Dropping.IsCompleted, "the set's drop did not finish");
                        for (int k = 0; k < sheets.Count; k++)
                        {
                            window.ShowSetSheet(k);
                            window.ApplyDetection(sheets[k].Result);
                            window.Session.SetCalibre(Calibre.Of(0.308));
                            window.Session.SetShotDistance(3600);
                            window.Session.SetEquipment(rifle, null, "Test load");
                            window.CalibreAnswered();
                            window.SaveWaitingNow();
                            window.BackToSet();
                        }

                        foreach (var (theme, name) in new[] { (ThemeChoice.Dark, "dark"), (ThemeChoice.Light, "light") })
                        {
                            window.SetTheme(theme);
                            Dispatcher.UIThread.RunJobs();
                            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);
                            Save(window, $"set-tab-{name}-{width}x{height}");
                        }
                    }
                    finally
                    {
                        GroupLab.Tests.Support.Temp.Delete(setFolder);
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
                        Width = width, Height = height, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true, AskScope = true,
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

                    // Entry 294 section 2: the Features page's own picture of "Works in your scope's unit", the question alone.
                    string scopeOnly = Path.Combine(Path.GetTempPath(), $"grouplab-entry294-{Guid.NewGuid():N}");
                    var asked = new MainWindow(new AppSettingsStore(Path.Combine(scopeOnly, "settings.json"))) { Width = width, Height = height, AskScope = true };
                    try
                    {
                        asked.Show();
                        asked.SetTheme(theme);
                        asked.ShowFirstRunIfDue();
                        Assert.True(asked.FirstRunShown);
                        Save(asked, $"scope-unit-{name}-{width}x{height}");
                    }
                    finally
                    {
                        asked.Close();
                        GroupLab.Tests.Support.Temp.Delete(scopeOnly);
                    }

                    // Entry 439 section 4.3: the guided demo, at its step on the plot, for the Features page and the user guide.
                    string demoFolder = Path.Combine(Path.GetTempPath(), $"grouplab-entry439-{Guid.NewGuid():N}");
                    var demo = new MainWindow(new AppSettingsStore(Path.Combine(demoFolder, "settings.json"))) { Width = width, Height = height };
                    try
                    {
                        demo.Show();
                        demo.SetTheme(theme);
                        var started = demo.StartDemo(5);
                        for (var until = DateTime.UtcNow.AddMinutes(1); !started.IsCompleted && DateTime.UtcNow < until;)
                        {
                            Dispatcher.UIThread.RunJobs();
                            Thread.Sleep(5);
                        }

                        started.GetAwaiter().GetResult();
                        Assert.True(demo.DemoRunning);
                        Save(demo, $"demo-tour-{name}-{width}x{height}");
                        demo.EndDemo(keep: false);
                    }
                    finally
                    {
                        demo.Close();
                        GroupLab.Tests.Support.Temp.Delete(demoFolder);
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

    internal const double PlainDpi = 150;

    internal const int PlainOuter = 150;

    /// <summary>The plain sample's four aim points, in its pixels: two by two on a Letter page at 150 dpi.</summary>
    internal static readonly PointD[] PlainCentres = [new(425, 560), new(850, 560), new(425, 1110), new(850, 1110)];

    /// <summary>Five shots on each bull, where the sample's holes are drawn.</summary>
    internal static readonly (int X, int Y)[] PlainHoles =
    [
        (440, 548), (409, 575), (431, 590), (452, 566), (417, 541),
        (866, 552), (839, 571), (858, 584), (871, 565), (844, 543),
        (433, 1098), (409, 1121), (447, 1127), (425, 1093), (452, 1112),
        (861, 1101), (835, 1119), (872, 1124), (848, 1094), (866, 1115),
    ];

    /// <summary>
    /// Entry 274, answering question 66: a sample target GroupLab draws itself, plainly not a GroupLab sheet: Letter at 150 dpi, four plain
    /// ring bulls with a solid center, no markers and no codes, five holes in each, and the words "Sample target" along the bottom. It carries
    /// nobody's design, so it may be shown anywhere.
    /// </summary>
    /// <summary>The stand-in for a store-bought bullseye: an 8 by 8 in sheet at this many pixels an inch, its bull at the middle.</summary>
    private const double StandInDpi = 100, StandInBull = 400;

    /// <summary>Entry 356: a GroupLab sheet printed without its codes, at 200 dpi, for the problem dialog's picture.</summary>
    private static string CodelessSheet()
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(AppContext.BaseDirectory, "targets", "GL-RF25-LTR.gltd.json")).Definition!;
        var page = GroupLab.Core.Rendering.SceneBuilder.Build(definition).Pages[0];
        var codeless = page with { Items = [.. page.Items.Where(i => i.Layer != GroupLab.Core.Rendering.SceneLayer.Codes)] };
        var render = GroupLab.Core.Rendering.SceneRasterizer.Rasterize(codeless, 200);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-codeless-{Guid.NewGuid():N}.png");
        using var mat = Mat.FromPixelData(render.Height, render.Width, MatType.CV_8UC1, render.Pixels);
        Cv2.ImWrite(path, mat);
        return path;
    }

    /// <summary>
    /// Entries 340 and 341: a stand-in for a store-bought bullseye, drawn by GroupLab with nothing of any maker's artwork: a black disc with
    /// light rings and a red center on plain paper, with its own words, so the picture of recognition publishes no one else's printing.
    /// </summary>
    private static string StoreTargetStandIn()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-standin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "store-bought-stand-in.png");
        using var image = new Mat(800, 800, MatType.CV_8UC3, new Scalar(236, 238, 240));
        var middle = new OpenCvSharp.Point(StandInBull, StandInBull);
        Cv2.Circle(image, middle, 300, new Scalar(35, 35, 35), -1, LineTypes.AntiAlias);
        foreach (int r in new[] { 240, 180, 120 })
        {
            Cv2.Circle(image, middle, r, new Scalar(200, 200, 200), 2, LineTypes.AntiAlias);
        }

        Cv2.Circle(image, middle, 45, new Scalar(40, 40, 200), -1, LineTypes.AntiAlias);
        Cv2.PutText(image, "Stand-in drawn by GroupLab", new OpenCvSharp.Point(200, 770), HersheyFonts.HersheySimplex, 0.8, new Scalar(90, 90, 90), 2, LineTypes.AntiAlias);
        Cv2.ImWrite(file, image);
        return file;
    }

    internal static string PlainSample()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-plain-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "sample-target.png");
        using var image = new Mat((int)(11 * PlainDpi), (int)(8.5 * PlainDpi), MatType.CV_8UC3, new Scalar(238, 240, 242));
        foreach (var c in PlainCentres)
        {
            var at = new OpenCvSharp.Point(c.X, c.Y);
            foreach (int r in new[] { PlainOuter, 110, 70 })
            {
                Cv2.Circle(image, at, r, new Scalar(40, 40, 40), 3, LineTypes.AntiAlias);
            }

            Cv2.Circle(image, at, 34, new Scalar(30, 30, 30), -1, LineTypes.AntiAlias);
        }

        foreach (var (x, y) in PlainHoles)
        {
            Cv2.Circle(image, new OpenCvSharp.Point(x, y), 7, new Scalar(150, 150, 150), -1, LineTypes.AntiAlias);
            Cv2.Circle(image, new OpenCvSharp.Point(x, y), 5, new Scalar(25, 25, 25), -1, LineTypes.AntiAlias);
        }

        Cv2.PutText(image, "Sample target", new OpenCvSharp.Point(500, 1560), HersheyFonts.HersheySimplex, 1.0, new Scalar(60, 60, 60), 2, LineTypes.AntiAlias);
        Cv2.ImWrite(file, image);
        return file;
    }

    /// <summary>
    /// Entry 256: a Features entry that names a bull, a grid or a sheet shows that thing itself, drawn from the library as it prints, cropped
    /// to it with a little margin, on its own white paper so one picture reads in both themes. The grids keep their legend and the numbers
    /// outside them, so the scale can be read.
    /// </summary>
    internal static void SheetPictures(IReadOnlyList<string> outputs)
    {
        const double dpi = 200;
        const double perDmm = dpi / 254;

        static GroupLab.Core.Gltd.Model.TargetDefinition Load(string file) =>
            GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", file)).Definition!;

        // Entry 300 section 5: beside each picture of part of a page, a bull or a grid, the same as vectors from the same scene, which the
        // site serves instead of the picture so it is sharp on any screen. A whole page stays a picture: as vectors it is ten times the size.
        string? drawing = null;
        GrayImage Draw(GroupLab.Core.Gltd.Model.TargetDefinition d, int page, double left, double top, double right, double bottom)
        {
            var scene = SceneBuilder.Build(d).Pages[page];
            left = Math.Max(0, left);
            top = Math.Max(0, top);
            right = Math.Min(scene.Width / 2.0, right);
            bottom = Math.Min(scene.Height / 2.0, bottom);
            drawing = SceneSvg.Write(scene, left, top, right, bottom);
            var region = new PixelRegion((int)Math.Round(left * perDmm), (int)Math.Round(top * perDmm), (int)Math.Round((right - left) * perDmm), (int)Math.Round((bottom - top) * perDmm));
            return SceneRasterizer.Rasterize(scene, dpi, 1.0, region, words: true);
        }

        void Save(string name, GrayImage image)
        {
            using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
            foreach (string output in outputs)
            {
                Cv2.ImWrite(Path.Combine(output, $"sheet-{name}.png"), mat);
                if (drawing is not null)
                {
                    File.WriteAllText(Path.Combine(output, $"sheet-{name}.svg"), drawing);
                }
            }

            drawing = null;
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
            string name = "page-" + file.Replace("GL-", "", StringComparison.Ordinal).ToLowerInvariant();
            Save(name, SceneRasterizer.Rasterize(SceneBuilder.Build(d).Pages[0], 40, words: true));
        }

        // Entry 273: the printer check page, whole, as it prints on Letter.
        Save("scale-check", SceneRasterizer.Rasterize(SceneBuilder.Build(Load("GL-SCALE-LTR-1.gltd.json")).Pages[0], 60, words: true));

        // A set of sheets, whole, side by side as they are laid out to shoot.
        void SetPicture(string name, string file, double dpi)
        {
            var d = Load(file);
            var pages = SceneBuilder.Build(d).Pages;
            var drawn = pages.Select(p => SceneRasterizer.Rasterize(p, dpi, words: true)).ToList();
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

            Save(name, new GrayImage(width, (rows * h) + ((rows - 1) * gap), all));
        }

        // A large format set: its four Letter sheets.
        SetPicture("large-set", "GL-LR25-T.gltd.json", 40);

        // Entry 289: the 2 MOA page, whole, as it prints on Letter, and its set of three pages.
        Save("two-moa", SceneRasterizer.Rasterize(SceneBuilder.Build(Load("GL-CF9-LTR.gltd.json")).Pages[0], 60, words: true));
        SetPicture("two-moa-set", "GL-CF9-T.gltd.json", 40);

        // Entry 358 section 8: a six-bull label on 4x6 as it prints on paper, and the same label as a 203 dpi thermal printer's dots, one
        // picture pixel to a dot, so the whole-dot codes and the black-only bulls can be seen.
        var label = SceneBuilder.Build(Load("GL-X6-4X6.gltd.json")).Pages[0];
        Save("x6-4x6", SceneRasterizer.Rasterize(label, 100, words: true));
        Save("x6-4x6-thermal", GroupLab.Core.Printing.Thermal.ThermalRaster.Render(label, new GroupLab.Core.Printing.Thermal.PrintHead(203.2, 832)).Image.ToGray());
    }
}
