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
            Assert.Contains(window.StatisticsText, t => t == (meanRadius * 2.54).ToString("F2", CultureInfo.InvariantCulture) + " cm");
            Assert.Contains(window.StatisticsText, t => t == "Angular figures need the shot distance.");

            window.Session.SetShotDistance(UnitSettings.DistanceToInches(100, DistanceUnit.Metre));
            string mil = UnitSettings.Metric.AngleText(meanRadius, window.Session.State.ShotDistanceInches)!;
            Assert.EndsWith(" mil", mil, StringComparison.Ordinal);
            Assert.Contains(window.StatisticsText, t => t.StartsWith(mil + ", interval", StringComparison.Ordinal));
            string written = MarkingFile.Write(window.Session.State);

            window.SetUnits(UnitSettings.Imperial);
            Assert.Contains(window.StatisticsText, t => t == meanRadius.ToString("F3", CultureInfo.InvariantCulture) + " in");
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

            var target = window.Session.State.Shots[1];
            rows[1].Children.OfType<Button>().Single(b => (b.Content as string) == "Not a shot").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.Session.State.Find(target.Id)!.NotAShot);

            window.ShotList.Children.OfType<Grid>().ElementAt(1).Children.OfType<Button>().Single(b => (b.Content as string) == "It is a shot").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
            Assert.Equal(ids, Rows().Select(b => (int)b.Tag!));
            Assert.Equal("2  bull none", ((TextBlock)Rows()[1].Content!).Text);

            Rows()[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(ids[1], window.Canvas.Selected);

            window.ShotList.GetLogicalDescendants().OfType<Button>().Where(b => (b.Content as string) == "Exclude").ElementAt(2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(ExclusionReason.CalledFlyer, window.Session.State.Find(ids[2])!.Exclusion);
            Assert.Equal("3  bull none, excluded as CalledFlyer", ((TextBlock)Rows()[2].Content!).Text);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
