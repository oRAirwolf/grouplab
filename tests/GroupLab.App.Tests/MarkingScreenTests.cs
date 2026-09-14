using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

[assembly: AvaloniaTestApplication(typeof(GroupLab.App.Tests.TestApplication))]

namespace GroupLab.App.Tests;

/// <summary>The application headless, with the same App and theme as the desktop build.</summary>
public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<GroupLab.App.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
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

    [AvaloniaFact]
    public void TapsSetAScaleAndMarkAGroupWhoseHeadlineIsTheEngineMeanRadius()
    {
        (int X, int Y)[] holes = [(300, 300), (340, 280), (320, 340)];
        string path = SyntheticTarget(holes);
        try
        {
            var window = new MainWindow { Width = 1400, Height = 900 };
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
            foreach (var (x, y) in holes)
            {
                Tap(new PointD(x + 3, y - 2));
            }

            var state = window.Session.State;
            Assert.Equal(3, state.Shots.Count);
            Assert.NotNull(state.PointOfAim);
            Assert.All(state.Shots, s => Assert.Equal(ShotProvenance.Manual, s.Provenance));

            // Each tap snapped back onto its hole.
            foreach (var (shot, hole) in state.Shots.Zip(holes))
            {
                Assert.True(Math.Abs(shot.Image.X - hole.X) < 1.5 && Math.Abs(shot.Image.Y - hole.Y) < 1.5, $"shot at ({shot.Image.X:0.0}, {shot.Image.Y:0.0}) for the hole at {hole}");
            }

            var report = GroupAnalysis.Analyse(state);
            var expected = GroupLab.Core.Statistics.GroupStatistics.Rayleigh([.. holes.Select(h => new PointD(h.X / 100.0, h.Y / 100.0))]);
            Assert.Equal(expected.MeanRadius.Value, report.AllShots!.MeanRadius.Value, 2);

            window.Session.Undo();
            Assert.Equal(2, window.Session.State.Shots.Count);
            window.Close();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
