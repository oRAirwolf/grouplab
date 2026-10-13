using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 417, Quack on macOS: with the Select tool a hole could no longer be dragged. A click on a bull with no hole
/// marked on it, as a missed hole's bull is, chooses that bull (entry 407), and from then on every press on a hole gave it or took it away.
/// Now a press on a hole with a bull chosen still drags it, and a tap where it was pressed still gives it, as entry 407 meant.
/// </summary>
public class Entry417Tests
{
    private static Point At(MainWindow window, PointD image) => window.Canvas.TranslatePoint(window.Canvas.ToControl(image), window)!.Value;

    [AvaloniaFact]
    public void WithABullChosenAHoleIsStillDraggedAndATapStillGivesIt()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Select;
            canvas.FitToView();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var state = window.Session.State;
            var first = state.Bulls.First(b => b.Scoring);
            var hole = state.Shots.First(s => s.IsShot && s.Bull is { } b && b != first.Index);
            canvas.Pick(first.Index);

            // A tap on the hole, let go where it was pressed: entry 407's tap gives it to the chosen bull.
            var on = At(window, hole.Image);
            window.MouseDown(on, MouseButton.Left);
            window.MouseUp(on, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(first.Index, window.Session.State.Find(hole.Id)!.Bull);
            Assert.Equal(first.Index, canvas.PickingBull);

            // A press and a drag moves it, with the bull still chosen, and gives or takes nothing.
            var before = window.Session.State.Find(hole.Id)!;
            var to = on + new Vector(30, 20);
            window.MouseDown(on, MouseButton.Left);
            window.MouseMove(on + new Vector(10, 7));
            window.MouseMove(to);
            window.MouseUp(to, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            var after = window.Session.State.Find(hole.Id)!;
            Assert.True(Math.Abs(after.Image.X - before.Image.X) > 1 || Math.Abs(after.Image.Y - before.Image.Y) > 1, "the hole did not move");
            var expected = canvas.ToImage(window.TranslatePoint(to, canvas)!.Value);
            Assert.Equal(expected.X, after.Image.X, 0);
            Assert.Equal(expected.Y, after.Image.Y, 0);
            Assert.Equal(first.Index, after.Bull);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
