using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using System.Globalization;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Controls;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

[assembly: AvaloniaTestApplication(typeof(GroupLab.App.Tests.TestApplication))]

namespace GroupLab.App.Tests;

/// <summary>
/// The application headless, with the same App and theme as the desktop build, drawn through Skia rather than the headless null renderer
/// so that a frame can be captured as a screenshot (NOTES-FROM-PLANNING.md entry 42 section 7).
/// </summary>
public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<GroupLab.App.App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// The marking screen driven by taps, NOTES-FROM-PLANNING.md entry 21 section 3 and docs/PHASE1-BRIEF.md section 6: open an image,
/// set a reference length with two taps, mark the point of aim, tap three impacts, and the headline figure is the engine's mean
/// radius. Taps go through the headless platform's pointer input, so the canvas's own handling is what is tested.
/// </summary>
public class MarkingScreenTests
{
    /// <summary>A light 800 by 600 target with three dark holes, written where the test can open it.</summary>
    private static string SyntheticTarget(IReadOnlyList<(int X, int Y)> holes)
    {
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-marking-{Guid.NewGuid():N}.png");
        using var image = new Mat(600, 800, MatType.CV_8UC3, new Scalar(235, 235, 235));
        foreach (var (x, y) in holes)
        {
            Cv2.Circle(image, new OpenCvSharp.Point(x, y), 9, new Scalar(30, 30, 30), -1);
        }

        Cv2.ImWrite(path, image);
        return path;
    }

    /// <summary>A window whose settings live in a file of the test's own, in the given units, so no test reads or writes the user's settings.</summary>
    private static MainWindow NewWindow(UnitSettings? units = null)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(units ?? UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 25 section 1: the unit setting changes what is shown and nothing that is stored. In centimetres the
    /// scale is entered in centimetres and the headline is in centimetres; with a shot distance the angular figure appears, in mil; in
    /// inches the same marking writes the same file; and the choice is remembered.
    /// </summary>
    [AvaloniaFact]
    public void UnitsChangeWhatIsShownAndNothingThatIsStored()
    {
        (int X, int Y)[] holes = [(300, 300), (340, 280), (320, 340), (250, 340), (390, 310)];
        string path = SyntheticTarget(holes);
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppSettingsStore(settings);
            store.SaveUnits(UnitSettings.Metric);
            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.OpenImage(path);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var canvas = window.Canvas;
            canvas.FitToView();
            Assert.Equal(UnitSettings.Metric, window.Units);

            void Tap(PointD image)
            {
                var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
                window.MouseDown(at, MouseButton.Left);
                window.MouseUp(at, MouseButton.Left);
            }

            // Two taps 200 px apart entered as 5.08 cm: 2 in, stored in inches.
            canvas.Tool = MarkingTool.Length;
            Tap(new PointD(100, 100));
            Tap(new PointD(300, 100));
            Assert.Contains(window.ScaleInputs.GetLogicalDescendants().OfType<TextBlock>(), t => (t.Text ?? "").Contains("cm:", StringComparison.Ordinal));
            window.ScaleInputs.GetLogicalDescendants().OfType<TextBox>().Single().Text = "5.08";
            window.ScaleInputs.GetLogicalDescendants().OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(2, Assert.IsType<LengthReference>(window.Session.State.Scale).Inches, 12);

            canvas.Tool = MarkingTool.Impact;
            foreach (var (x, y) in holes)
            {
                Tap(new PointD(x + 3, y - 2));
            }

            double meanRadius = GroupAnalysis.Analyse(window.Session.State).AllShots!.MeanRadius!.Value;
            Assert.Contains(window.StatisticsText, t => t.StartsWith((meanRadius * 2.54).ToString("F2", CultureInfo.InvariantCulture) + " cm", StringComparison.Ordinal));
            Assert.Contains(window.StatisticsText, t => t == "Angular figures need the shot distance.");

            window.Session.SetShotDistance(UnitSettings.DistanceToInches(100, DistanceUnit.Metre));
            string mil = UnitSettings.Metric.AngleText(meanRadius, window.Session.State.ShotDistanceInches)!;
            Assert.EndsWith(" mil", mil, StringComparison.Ordinal);
            Assert.Contains(window.StatisticsText, t => t.EndsWith(mil, StringComparison.Ordinal));
            string written = MarkingFile.Write(window.Session.State);

            window.SetUnits(UnitSettings.Imperial);
            Assert.Contains(window.StatisticsText, t => t.StartsWith(meanRadius.ToString("F3", CultureInfo.InvariantCulture) + " in", StringComparison.Ordinal));
            Assert.Equal(written, MarkingFile.Write(window.Session.State));
            window.Close();
            Assert.Equal(UnitSettings.Imperial, new AppSettingsStore(settings).LoadUnits());
        }
        finally
        {
            File.Delete(path);
            File.Delete(settings);
        }
    }

    [AvaloniaFact]
    public void TapsSetAScaleAndMarkAGroupWhoseHeadlineIsTheEngineMeanRadius()
    {
        (int X, int Y)[] holes = [(300, 300), (340, 280), (320, 340), (250, 340), (390, 310)];
        string path = SyntheticTarget(holes);
        try
        {
            var window = NewWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.OpenImage(path);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var canvas = window.Canvas;
            canvas.FitToView();

            void Tap(PointD image)
            {
                var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
                window.MouseDown(at, MouseButton.Left);
                window.MouseUp(at, MouseButton.Left);
            }

            // Two taps 200 px apart, entered as 2 in: 100 px to the inch.
            canvas.Tool = MarkingTool.Length;
            PointD[] length = [];
            canvas.LengthTapped += (_, taps) => length = [.. taps];
            Tap(new PointD(100, 100));
            var probe = canvas.TranslatePoint(canvas.ToControl(new PointD(100, 100)), window)!.Value;
            var hit = window.InputHitTest(probe);
            Assert.True(canvas.PendingTaps.Count == 1, $"canvas bounds {canvas.Bounds}, tap at {probe}, hit {hit?.GetType().Name ?? "nothing"}, pending {canvas.PendingTaps.Count}");
            Tap(new PointD(300, 100));
            Assert.Equal(2, length.Length);
            window.Session.SetScale(new LengthReference(length[0], length[1], 2));

            canvas.Tool = MarkingTool.Aim;
            Tap(new PointD(320, 310));

            canvas.Tool = MarkingTool.Impact;
            foreach (var (x, y) in holes.Take(4))
            {
                Tap(new PointD(x + 3, y - 2));
            }

            // Four shots: entry 24 section 1, the panel says what is missing and prints no mean radius.
            Assert.Null(GroupAnalysis.Analyse(window.Session.State).AllShots!.MeanRadius);
            Assert.Contains(window.StatisticsText, t => t.Contains("At least 5", StringComparison.Ordinal));
            Assert.DoesNotContain(window.StatisticsText, t => t == "Mean radius");

            Tap(new PointD(holes[4].X + 3, holes[4].Y - 2));
            var state = window.Session.State;
            Assert.Equal(5, state.Shots.Count);
            Assert.Contains(window.StatisticsText, t => t == "Mean radius");
            Assert.DoesNotContain(window.StatisticsText, t => t.Contains("95%", StringComparison.Ordinal) && !t.Contains("95.0%", StringComparison.Ordinal));
            Assert.NotNull(state.PointOfAim);
            Assert.All(state.Shots, s => Assert.Equal(ShotProvenance.Manual, s.Provenance));

            // Each tap snapped back onto its hole.
            foreach (var (shot, hole) in state.Shots.Zip(holes))
            {
                Assert.True(Math.Abs(shot.Image.X - hole.X) < 1.5 && Math.Abs(shot.Image.Y - hole.Y) < 1.5, $"shot at ({shot.Image.X:0.0}, {shot.Image.Y:0.0}) for the hole at {hole}");
            }

            var report = GroupAnalysis.Analyse(state);
            var expected = GroupLab.Core.Statistics.GroupStatistics.Rayleigh([.. holes.Select(h => new PointD(h.X / 100.0, h.Y / 100.0))]);
            Assert.Equal(expected.MeanRadius.Value, report.AllShots!.MeanRadius!.Value, 2);

            window.Session.Undo();
            Assert.Equal(4, window.Session.State.Shots.Count);
            Assert.Contains(window.StatisticsText, t => t.Contains("At least 5", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 26's test: mark shots, turn the view, and every shot's stored and target position is unchanged while
    /// every drawn position has turned with the image. A tap on a hole after the turn still lands on that hole, and undo turns the view
    /// back to where every mark is drawn exactly as before.
    /// </summary>
    [AvaloniaFact]
    public void RotatingTheViewMovesNoMarkAndTurnsWhereEveryMarkIsDrawn()
    {
        (int X, int Y)[] holes = [(300, 300), (340, 280), (320, 340), (250, 340), (390, 310)];
        string path = SyntheticTarget(holes);
        try
        {
            var window = NewWindow();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.OpenImage(path);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var canvas = window.Canvas;
            canvas.FitToView();

            void Tap(PointD image)
            {
                var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
                window.MouseDown(at, MouseButton.Left);
                window.MouseUp(at, MouseButton.Left);
            }

            window.Session.SetScale(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2));
            canvas.Tool = MarkingTool.Impact;
            foreach (var (x, y) in holes.Take(4))
            {
                Tap(new PointD(x + 3, y - 2));
            }

            var before = window.Session.State;
            var targets = before.Shots.Select(s => before.Scale!.ToTarget(s.Image)).ToList();
            var drawn = before.Shots.Select(s => canvas.ToControl(s.Image)).ToList();

            window.Session.Rotate(1);
            Dispatcher.UIThread.RunJobs();
            var after = window.Session.State;
            Assert.Equal(1, after.ViewQuarterTurns);
            Assert.Equal(before.Shots, after.Shots);
            Assert.Equal(targets, after.Shots.Select(s => after.Scale!.ToTarget(s.Image)));

            // The drawn image is turned a quarter clockwise: its top left corner is now drawn at the top right, its bottom left at the top left.
            var topLeft = canvas.ToControl(new PointD(0, 0));
            var bottomLeft = canvas.ToControl(new PointD(0, 600));
            var topRight = canvas.ToControl(new PointD(800, 0));
            Assert.Equal(topLeft.Y, bottomLeft.Y, 6);
            Assert.True(topLeft.X > bottomLeft.X);
            Assert.Equal(topLeft.X, topRight.X, 6);
            Assert.True(topRight.Y > topLeft.Y);

            // Every drawn displacement (dx, dy) between marks is now (-dy, dx), scaled by the zoom that refits the turned image.
            var turned = after.Shots.Select(s => canvas.ToControl(s.Image)).ToList();
            static double Length(Vector v) => Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
            double scale = Length(turned[1] - turned[0]) / Length(drawn[1] - drawn[0]);
            for (int i = 1; i < drawn.Count; i++)
            {
                Vector was = drawn[i] - drawn[0], now = turned[i] - turned[0];
                Assert.Equal(-was.Y * scale, now.X, 6);
                Assert.Equal(was.X * scale, now.Y, 6);
            }

            Tap(new PointD(holes[4].X + 3, holes[4].Y - 2));
            var fifth = window.Session.State.Shots[^1];
            Assert.True(Math.Abs(fifth.Image.X - holes[4].X) < 1.5 && Math.Abs(fifth.Image.Y - holes[4].Y) < 1.5, $"shot at ({fifth.Image.X:0.0}, {fifth.Image.Y:0.0}) for the hole at {holes[4]}");

            window.Session.Undo();
            window.Session.Undo();
            Assert.Equal(0, window.Session.State.ViewQuarterTurns);
            Assert.Equal(drawn, window.Session.State.Shots.Select(s => canvas.ToControl(s.Image)));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A window showing the image at the path, fitted, and a tap that goes through the headless platform's pointer input.</summary>
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 98 section 2: the snap covers the same paper at every zoom. Its reach is one hole's extent in sheet units
    /// plus a pointing tolerance of a few screen pixels, so zooming in by four changes it by less than that tolerance, where a reach in screen
    /// pixels alone would have shrunk to a quarter. And a click inside a hole's drawn ring selects it however far the view is zoomed.
    /// </summary>
    [AvaloniaFact]
    public void TheSnapCoversTheSamePaperAtEveryZoomAndTheRingIsClickable()
    {
        (int X, int Y)[] holes = [(300, 300)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            var canvas = window.Canvas;
            var session = window.Session;
            session.SetScale(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2));
            var at = new PointD(300, 300);
            double physical = HoleSize.SnapRadiusPixels(session.State, at)!.Value;
            Assert.Equal(HoleSize.NominalHoleInches * 100, physical, 6);

            double before = canvas.SnapRadius(session.State, at);
            canvas.ZoomBy(4);
            double after = canvas.SnapRadius(session.State, at);
            Assert.True(before - after < 4, $"the reach went from {before:0.0} to {after:0.0} image pixels on zooming in");
            Assert.True(after >= physical, "the reach is never less than one hole");

            // Once the detector has measured the sheet's holes, their median is the hole the snap is sized to.
            session.LoadDetections(session.State.Scale!, [new BullAim(0, "1", at)],
                [new DetectedShot(at, new GroupLab.Core.Detection.AssignedShot(0, 0, 0, 0, 0, double.PositiveInfinity, false), 0.22)], null, [], "test");
            Assert.Equal(22, HoleSize.SnapRadiusPixels(session.State, at)!.Value, 6);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static (MainWindow Window, Action<PointD> Tap) Opened(string path)
    {
        var window = NewWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.OpenImage(path);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var canvas = window.Canvas;
        canvas.FitToView();
        return (window, image =>
        {
            var at = canvas.TranslatePoint(canvas.ToControl(image), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
        });
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 39 section 1: on a sheet of bulls every tapped impact is assigned to its nearest bull, and when one is
    /// not, the panel prints no figure and says what is missing where the figures would be.
    /// </summary>
    [AvaloniaFact]
    public void OnASheetOfBullsTappedShotsAreAssignedAndAnUnassignedShotWithholdsTheFigures()
    {
        (int X, int Y)[] holes = [(200, 200), (236, 180), (210, 238), (500, 200), (464, 226), (536, 186)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, tap) = Opened(path);
            window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), [new BullAim(0, "1", new PointD(215, 205)), new BullAim(1, "2", new PointD(500, 205))], [], "test");
            window.Canvas.Tool = MarkingTool.Impact;
            foreach (var (x, y) in holes)
            {
                tap(new PointD(x + 2, y - 2));
            }

            var shots = window.Session.State.Shots;
            Assert.Equal([0, 0, 0, 1, 1, 1], shots.Select(s => s.Bull ?? -1));
            Assert.Contains(window.StatisticsText, t => t == "Mean radius");

            window.Session.AssignBull(shots[4].Id, null);
            Assert.DoesNotContain(window.StatisticsText, t => t == "Mean radius");
            Assert.Contains(window.StatisticsText, t => t.StartsWith("1 of 6 shots is not assigned to a bull", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Waits for the window's detection, pumping the UI thread its continuations run on.</summary>
    private static void Pump(MainWindow window)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(window.DetectionTask is { IsCompleted: true }, "the detection did not finish");
    }

    /// <summary>A rendered GL-CF25-LTR, written as an image file, for the tests that need a sheet the application recognises.</summary>
    private static string RenderedSheet()
    {
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Path.Combine(AppContext.BaseDirectory, "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        var sheet = GroupLab.Core.Rendering.SceneRasterizer.Rasterize(GroupLab.Core.Rendering.SceneBuilder.Build(definition).Pages[0], 300);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-sheet-{Guid.NewGuid():N}.png");
        using var mat = Mat.FromPixelData(sheet.Height, sheet.Width, MatType.CV_8UC1, sheet.Pixels);
        Cv2.ImWrite(path, mat);
        return path;
    }

    private static MainWindow DetectingWindow()
    {
        var window = NewWindow();
        window.DetectOnOpen = true;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 76 section 4: opening a GroupLab sheet detects on it without anybody asking, and opening an image that is not
    /// one does nothing and says so, with no definition asked for.
    /// </summary>
    [AvaloniaFact]
    public void OpeningARecognisedSheetDetectsAndAnythingElseSaysItWasNotDetected()
    {
        string sheet = RenderedSheet();
        string plain = SyntheticTarget([(300, 300)]);
        try
        {
            var window = DetectingWindow();
            window.OpenImage(sheet);
            Pump(window);
            Assert.IsType<SheetReference>(window.Session.State.Scale);
            Assert.NotNull(window.Session.State.RegistrationSummary);

            window.OpenImage(plain);
            Pump(window);
            Assert.Null(window.Session.State.Scale);
            Assert.Contains("not a GroupLab sheet GroupLab recognises", window.StatusText, StringComparison.Ordinal);
            window.Close();
        }
        finally
        {
            File.Delete(sheet);
            File.Delete(plain);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 78 section 4: the calibre is used in finding holes. Named after a detection nobody has corrected, it detects
    /// again; named after a correction, it says the next Detect would use it and leaves the marks alone; and it stays named for the next sheet.
    /// </summary>
    [AvaloniaFact]
    public void NamingACalibreDetectsAgainOnlyWhileTheMarksAreUntouched()
    {
        string sheet = RenderedSheet();
        try
        {
            var window = DetectingWindow();
            window.OpenImage(sheet);
            Pump(window);
            var first = window.DetectionTask;

            window.EnterCalibre(".308");
            Assert.NotSame(first, window.DetectionTask);
            Assert.Equal(MainWindow.CalibreRedetectText, window.StatusText);
            Pump(window);
            Assert.IsType<SheetReference>(window.Session.State.Scale);
            Assert.Equal(0.308, window.Session.State.Calibre!.DiameterInches, 6);

            var detected = window.DetectionTask;
            window.Session.AddShot(new PointD(600, 600));
            window.EnterCalibre(".223");
            Assert.Same(detected, window.DetectionTask);
            Assert.Equal(MainWindow.CalibreAfterCorrectionsText, window.StatusText);
            Assert.Contains(window.Session.State.Shots, s => s.Provenance == ShotProvenance.Manual);

            window.OpenImage(sheet);
            Assert.Equal(".223", window.Session.State.Calibre!.Name);
            Pump(window);
            window.Close();
        }
        finally
        {
            File.Delete(sheet);
        }
    }

    /// <summary>Entry 76 section 4: a detection started on opening can be cancelled, and then nothing of it is applied.</summary>
    [AvaloniaFact]
    public void ADetectionStartedOnOpeningCanBeCancelled()
    {
        string sheet = RenderedSheet();
        try
        {
            var window = DetectingWindow();
            window.OpenImage(sheet);
            window.CancelDetection();
            Assert.Equal(MainWindow.CancellingText, window.StatusText);
            Pump(window);
            Assert.Null(window.Session.State.Scale);
            Assert.StartsWith("Detection cancelled", window.StatusText, StringComparison.Ordinal);
            window.Close();
        }
        finally
        {
            File.Delete(sheet);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 83 section 4 and DESIGN.md section 13: the assignment editor. A shot the matching gave a bull other than its
    /// nearest is a contested item with its sentence and choices; Enter takes the first choice and pins it; undo brings the item back; a bull
    /// typed and Enter reassigns the selected shot, which here makes a new item, two shots on one bull; Discard edits puts back what detection
    /// found; and the counter says how many still need a decision throughout.
    /// </summary>
    [AvaloniaFact]
    public void TheReviewQueueSettlesAContestedShotFromTheKeyboard()
    {
        (int X, int Y)[] holes = [(200, 200), (330, 200)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            var session = window.Session;
            var scale = new LengthReference(new PointD(100, 100), new PointD(300, 100), 2);
            BullAim[] bulls = [new(0, "1", new PointD(215, 205)), new(1, "2", new PointD(515, 205))];
            var a = new GroupLab.Core.Detection.AssignedShot(0, 0, 40, 0, 40, 3000, false);
            var b = new GroupLab.Core.Detection.AssignedShot(1, 1, 480, 0, 300, 180, false);
            var assignment = new GroupLab.Core.Detection.ShotAssignmentResult(GroupLab.Core.Detection.AssignmentMethod.OneToOne, "as many shots as bulls", [a, b]);
            session.LoadDetections(scale, bulls, [new DetectedShot(new PointD(200, 200), a), new DetectedShot(new PointD(330, 200), b)], assignment, [], "test");
            window.RememberDetected();
            Dispatcher.UIThread.RunJobs();
            int contested = session.State.Shots[1].Id;

            Assert.True(window.ReviewText.Contains("1 of 2 need review"), string.Join(" | ", window.ReviewText));
            var item = window.CurrentReview!;
            Assert.Equal((ReviewKind.Contested, (int?)contested), (item.Kind, item.ShotId));
            Assert.Contains("One-to-one matching gives it to bull 2", item.Sentence, StringComparison.Ordinal);
            Assert.Equal(["Bull 2, as matched", "Bull 1", "Not a shot"], item.Choices.Select(c => c.Label));

            Press(window, Key.Enter);
            Assert.True(session.State.Find(contested)!.BullChosen);
            Assert.Equal(1, session.State.Find(contested)!.Bull);
            Assert.True(window.ReviewText.Contains("0 of 2 need review"), string.Join(" | ", window.ReviewText));

            session.Undo();
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.ReviewText.Contains("1 of 2 need review"), string.Join(" | ", window.ReviewText));

            window.Canvas.Selected = contested;
            Press(window, Key.D1);
            Press(window, Key.Enter);
            Assert.Equal((0, true), (session.State.Find(contested)!.Bull!.Value, session.State.Find(contested)!.BullChosen));

            // Putting it on bull 1 makes a new item, two shots on one bull, which Enter keeps.
            Assert.Equal(ReviewKind.Doubled, window.CurrentReview!.Kind);
            Assert.True(window.ReviewText.Contains("1 of 2 need review"), string.Join(" | ", window.ReviewText));
            Press(window, Key.Enter);
            Assert.True(window.ReviewText.Contains("0 of 2 need review"), string.Join(" | ", window.ReviewText));

            // Entry 103 section 1: Discard edits asks before discarding. Keeping them leaves every edit; confirming puts back what detection found.
            Button Named(string content) => window.GetLogicalDescendants().OfType<Button>().Single(x => Equals(x.Content, content));
            Named("Discard edits").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(session.State.Find(contested)!.BullChosen);
            Named("Keep them").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(session.State.Find(contested)!.BullChosen);
            Named("Discard edits").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Named("Discard").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.False(session.State.Find(contested)!.BullChosen);
            Assert.True(window.ReviewText.Contains("1 of 2 need review"), string.Join(" | ", window.ReviewText));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Press(MainWindow window, Key key)
    {
        window.OnReviewKey(window, new KeyEventArgs { Key = key, RoutedEvent = InputElement.KeyDownEvent, Source = window });
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 82 section 6: the detector's oversize flag reaches the marking screen, as a ring on the canvas and a sentence
    /// in the panel, quieter when tentative, and moving the shot clears it with the measurement it described.
    /// </summary>
    [AvaloniaFact]
    public void TheDetectorsOversizeFlagIsOnTheScreen()
    {
        (int X, int Y)[] holes = [(200, 200), (400, 200)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            var session = window.Session;
            var scale = new LengthReference(new PointD(100, 100), new PointD(300, 100), 2);
            var assigned = new GroupLab.Core.Detection.AssignedShot(0, 0, 0, 0, 0, double.PositiveInfinity, false);
            session.LoadDetections(scale, [new BullAim(0, "1", new PointD(215, 205))],
                [new DetectedShot(new PointD(200, 200), assigned, 0.52, new DetectedOversize(1.9, false)), new DetectedShot(new PointD(400, 200), assigned, 0.40, new DetectedOversize(1.5, true))], null, [], "test");
            Dispatcher.UIThread.RunJobs();
            var ids = session.State.Shots.Select(s => s.Id).ToList();
            Assert.Equal(new Dictionary<int, bool> { [ids[0]] = false, [ids[1]] = true }, window.Canvas.DetectorFlags);
            Assert.Contains(window.StatisticsText, t => t.Contains("covers about 1.9 holes' area", StringComparison.Ordinal));
            Assert.Contains(window.StatisticsText, t => t.Contains("may be two holes", StringComparison.Ordinal));

            session.MoveShot(ids[0], new PointD(205, 200));
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.Canvas.DetectorFlags.ContainsKey(ids[0]));
            Assert.DoesNotContain(window.StatisticsText, t => t.Contains("1.9 holes", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 76 section 4: a detected shot's ring is the diameter the detector measured, in sheet units at any zoom, with
    /// the calibre's hole drawn beside it once a calibre is set; a shot placed by hand, which has no measurement, keeps the calibre ring, and
    /// moving a detected shot drops the measurement.
    /// </summary>
    [AvaloniaFact]
    public void ADetectedShotIsDrawnAtItsMeasuredDiameterWithTheCalibreBesideIt()
    {
        (int X, int Y)[] holes = [(200, 200)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            var session = window.Session;
            var scale = new LengthReference(new PointD(100, 100), new PointD(300, 100), 2);
            var assigned = new GroupLab.Core.Detection.AssignedShot(0, 0, 0, 0, 0, double.PositiveInfinity, false);
            session.LoadDetections(scale, [new BullAim(0, "1", new PointD(215, 205))], [new DetectedShot(new PointD(200, 200), assigned, 0.52)], null, [], "test");
            int detected = session.State.Shots.Single().Id;
            int hand = session.AddShot(new PointD(260, 240));

            foreach (double factor in new[] { 1.0, 4.0 })
            {
                window.Canvas.ZoomBy(factor);
                var (impact, expected, _) = window.Canvas.RingDiametersInches(detected);
                Assert.Equal(0.52, impact, 6);
                Assert.Null(expected);
            }

            session.SetCalibre(new Calibre(".308", 0.308));
            var (measured, calibre, _) = window.Canvas.RingDiametersInches(detected);
            Assert.Equal((0.52, 0.308), (Math.Round(measured, 6), Math.Round(calibre!.Value, 6)));
            var (byHand, noExpected, _) = window.Canvas.RingDiametersInches(hand);
            Assert.Equal(0.308, byHand, 6);
            Assert.Null(noExpected);

            session.MoveShot(detected, new PointD(205, 200));
            Assert.Null(session.State.Find(detected)!.MeasuredDiameterInches);
            Assert.Equal(0.308, window.Canvas.RingDiametersInches(detected).Impact, 6);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 73 section 7 and DESIGN.md section 19: the headline figures stay in the panel, the reference figures are
    /// one click away, closed until opened, and a window opened later remembers that they were.
    /// </summary>
    [AvaloniaFact]
    public void TheReferenceFiguresSitBehindADisclosureThatRemembersItWasOpened()
    {
        (int X, int Y)[] holes = [(200, 200), (236, 180), (210, 238), (500, 200), (464, 226), (536, 186)];
        string path = SyntheticTarget(holes);
        string settings = Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppSettingsStore(settings);
            var window = new MainWindow(store);
            window.Show();
            window.OpenImage(path);
            window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), [new BullAim(0, "1", new PointD(215, 205)), new BullAim(1, "2", new PointD(500, 205))],
                [.. holes.Select(h => (new PointD(h.X, h.Y), (int?)(h.X < 350 ? 0 : 1)))], "test");
            Dispatcher.UIThread.RunJobs();

            string[] reference = [.. window.MoreFigures.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")];
            Assert.Contains(window.StatisticsText, t => t == "Mean radius");
            Assert.DoesNotContain(reference, t => t == "Mean radius");
            // Entry 103 section 2: the ellipse's reference is the round card's evidence now, beside the verdict, not behind the disclosure.
            Assert.DoesNotContain(reference, t => t.StartsWith("Error ellipse", StringComparison.Ordinal));
            Assert.Contains(window.JudgementCards, card => card.Any(t => t.StartsWith("Error ellipse", StringComparison.Ordinal) && t.Contains("circular shots give about", StringComparison.Ordinal)));
            Assert.False(window.MoreFigures.IsExpanded);
            Assert.False(store.LoadMoreFigures());

            window.MoreFigures.IsExpanded = true;
            Assert.True(store.LoadMoreFigures());
            window.Close();

            var later = new MainWindow(new AppSettingsStore(settings));
            Assert.True(later.MoreFigures.IsExpanded);
        }
        finally
        {
            File.Delete(path);
            File.Delete(settings);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 73 section 6: every row of the shot list fits inside the right column, the longest text giving way before
    /// any button is cut off, and a detection that is not a shot is taken out from its row and put back the same way.
    /// </summary>
    [AvaloniaFact]
    public void EveryShotListRowFitsTheColumnAndAFalsePositiveIsTakenOutFromItsRow()
    {
        (int X, int Y)[] holes = [(200, 200), (236, 180), (500, 200)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            window.Session.LoadDetections(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2), [new BullAim(0, "1", new PointD(215, 205)), new BullAim(1, "22", new PointD(500, 205))],
                [.. holes.Select(h => (new PointD(h.X, h.Y), (int?)1))], "test");
            window.Session.SetExclusion(window.Session.State.Shots[0].Id, ExclusionReason.CalledFlyer);
            Dispatcher.UIThread.RunJobs();

            double width = Tokens.RightColumnWidth - Tokens.SectionPadding.Left - Tokens.SectionPadding.Right;
            var rows = window.ShotList.Children.OfType<Grid>().ToList();
            Assert.Equal(holes.Length, rows.Count);
            foreach (var row in rows)
            {
                row.Measure(new Avalonia.Size(width, double.PositiveInfinity));
                row.Arrange(new Avalonia.Rect(0, 0, width, row.DesiredSize.Height));
                Assert.All(row.Children, c => Assert.True(c.Bounds.Right <= width + 0.5, $"{c.GetType().Name} ends at {c.Bounds.Right:0.0} in a {width:0.0} column"));
            }

            // Entry 75: three shots on bull 22 are 22a, 22b and 22c, top to bottom, and no row carries a bare index.
            Grid RowOf(int id) => window.ShotList.Children.OfType<Grid>().Single(r => r.Children.OfType<Button>().Any(b => b.Tag is int t && t == id));
            string TextOf(Grid row) => ((TextBlock)row.Children.OfType<Button>().Single(b => b.Tag is int).Content!).Text!;
            Assert.Equal(["22a", "22b, excluded as CalledFlyer", "22c"], rows.Select(TextOf));

            var target = window.Session.State.Shots[1];
            RowOf(target.Id).Children.OfType<Button>().Single(b => (b.Content as string) == "Not a shot").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.Session.State.Find(target.Id)!.NotAShot);
            Assert.StartsWith("not a shot, at ", TextOf(RowOf(target.Id)), StringComparison.Ordinal);

            RowOf(target.Id).Children.OfType<Button>().Single(b => (b.Content as string) == "It is a shot").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.Session.State.Find(target.Id)!.NotAShot);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 40 section 1: with the sheet's printed artwork known, a tap on printed ink is placed where it was tapped
    /// and the status line says why, while a tap by a hole in bare paper still snaps onto it.
    /// </summary>
    [AvaloniaFact]
    public void ATapOnKnownArtworkIsPlacedWhereTappedAndSaysSoWhileATapByAHoleSnaps()
    {
        (int X, int Y)[] holes = [(300, 300), (500, 300)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, tap) = Opened(path);
            var printed = Enumerable.Repeat((byte)255, 800 * 600).ToArray();
            for (int y = 280; y <= 320; y++)
            {
                for (int x = 280; x <= 320; x++)
                {
                    if (((x - 300) * (x - 300)) + ((y - 300) * (y - 300)) <= 144)
                    {
                        printed[(y * 800) + x] = 0;
                    }
                }
            }

            window.Canvas.Artwork = new GrayImage(800, 600, printed);
            window.Canvas.Tool = MarkingTool.Impact;
            tap(new PointD(303, 298));
            var onInk = window.Session.State.Shots[^1];
            Assert.True(Math.Abs(onInk.Image.X - 303) < 0.6 && Math.Abs(onInk.Image.Y - 298) < 0.6, $"placed at ({onInk.Image.X:0.00}, {onInk.Image.Y:0.00})");
            Assert.Contains("printed target", window.StatusText, StringComparison.Ordinal);

            tap(new PointD(503, 298));
            var hole = window.Session.State.Shots[^1];
            Assert.True(Math.Abs(hole.Image.X - 500) < 1.5 && Math.Abs(hole.Image.Y - 300) < 1.5, $"shot at ({hole.Image.X:0.0}, {hole.Image.Y:0.0})");
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Avalonia.Point At(MainWindow window, PointD image) => window.Canvas.TranslatePoint(window.Canvas.ToControl(image), window)!.Value;

    /// <summary>A press at one image point, a move to another, and a release there, through the headless pointer input.</summary>
    private static void Drag(MainWindow window, PointD from, PointD to)
    {
        window.MouseDown(At(window, from), MouseButton.Left);
        window.MouseMove(At(window, to));
        window.MouseUp(At(window, to), MouseButton.Left);
    }

    private static bool Near(PointD a, PointD b) => Math.Abs(a.X - b.X) < 0.6 && Math.Abs(a.Y - b.Y) < 0.6;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 39 section 3: the reference length is drawn while it is made, out to the pointer; it stays, waiting for
    /// its size, after the second tap; an end dragged before the length is used is the end used; and an end of the length in use can be
    /// dragged afterwards, as one step undo reverses.
    /// </summary>
    [AvaloniaFact]
    public void TheScaleLineIsDrawnAsItIsMadeAndItsEndsDragBeforeAndAfterItIsUsed()
    {
        string path = SyntheticTarget([(400, 300)]);
        try
        {
            var (window, tap) = Opened(path);
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Length;
            tap(new PointD(100, 100));
            Assert.Single(canvas.PendingTaps);
            window.MouseMove(At(window, new PointD(250, 110)));
            Assert.True(canvas.Hover is { } hover && Near(hover, new PointD(250, 110)), $"hover {canvas.Hover}");

            tap(new PointD(300, 100));
            Assert.Empty(canvas.PendingTaps);
            Assert.Equal(2, canvas.AwaitingTaps.Count);

            Drag(window, new PointD(300, 100), new PointD(320, 100));
            Assert.Empty(canvas.PendingTaps);
            Assert.True(Near(canvas.AwaitingTaps[1], new PointD(320, 100)), $"end at {canvas.AwaitingTaps[1]}");

            window.ScaleInputs.GetLogicalDescendants().OfType<TextBox>().Single().Text = "2.2";
            window.ScaleInputs.GetLogicalDescendants().OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var used = Assert.IsType<LengthReference>(window.Session.State.Scale);
            Assert.True(Near(used.B, new PointD(320, 100)), $"B at {used.B}");
            Assert.Empty(canvas.AwaitingTaps);

            canvas.Tool = MarkingTool.Select;
            Drag(window, new PointD(100, 100), new PointD(80, 100));
            Assert.True(Near(Assert.IsType<LengthReference>(window.Session.State.Scale).A, new PointD(80, 100)));
            window.Session.Undo();
            Assert.True(Near(Assert.IsType<LengthReference>(window.Session.State.Scale).A, new PointD(100, 100)));
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 39 section 4: an impact is placed by pressing, dragging to where it belongs and letting go, not tapped
    /// blind. A press on bare paper dragged onto a hole makes one shot, snapped onto that hole.
    /// </summary>
    [AvaloniaFact]
    public void AnImpactIsPlacedByPressingDraggingAndLettingGoAndSnapsWhereItIsLetGo()
    {
        (int X, int Y)[] holes = [(300, 300), (420, 300)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, _) = Opened(path);
            window.Canvas.Tool = MarkingTool.Impact;
            Drag(window, new PointD(250, 220), new PointD(416, 303));
            var shot = Assert.Single(window.Session.State.Shots);
            Assert.True(Math.Abs(shot.Image.X - 420) < 1.5 && Math.Abs(shot.Image.Y - 300) < 1.5, $"shot at ({shot.Image.X:0.0}, {shot.Image.Y:0.0})");
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 39 section 4: every shot is a row with its number and bull; a row selects its shot, and excludes it
    /// from the row without selecting it first.
    /// </summary>
    [AvaloniaFact]
    public void EveryShotIsARowThatSelectsItAndExcludesItFromTheRow()
    {
        (int X, int Y)[] holes = [(300, 300), (360, 280), (330, 350)];
        string path = SyntheticTarget(holes);
        try
        {
            var (window, tap) = Opened(path);
            window.Session.SetScale(new LengthReference(new PointD(100, 100), new PointD(300, 100), 2));
            window.Canvas.Tool = MarkingTool.Impact;
            foreach (var (x, y) in holes)
            {
                tap(new PointD(x, y));
            }

            var ids = window.Session.State.Shots.Select(s => s.Id).ToList();
            List<Button> Rows() => [.. window.ShotList.GetLogicalDescendants().OfType<Button>().Where(b => b.Tag is int)];

            // Entry 75: a plain group has no printed numbers, so its rows are in position order and named by where each shot is.
            Assert.Equal([ids[1], ids[0], ids[2]], Rows().Select(b => (int)b.Tag!));
            Assert.All(Rows(), r => Assert.StartsWith("at ", ((TextBlock)r.Content!).Text, StringComparison.Ordinal));

            Rows()[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(ids[0], window.Canvas.Selected);

            window.ShotList.GetLogicalDescendants().OfType<Button>().Where(b => (b.Content as string) == "Exclude").ElementAt(2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(ExclusionReason.CalledFlyer, window.Session.State.Find(ids[2])!.Exclusion);
            Assert.EndsWith(", excluded as CalledFlyer", ((TextBlock)Rows()[2].Content!).Text, StringComparison.Ordinal);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
