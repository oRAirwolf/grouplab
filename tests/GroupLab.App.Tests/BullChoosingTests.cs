using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 1 on the desktop: a click on a bull chooses it, a click on a hole then gives it to that bull or takes it
/// away, Escape ends it, and the switch hiding the lines is remembered.
/// </summary>
public class BullChoosingTests
{
    private static void Click(MainWindow window, Point onCanvas)
    {
        var at = window.Canvas.TranslatePoint(onCanvas, window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ClickingABullChoosesItAndClickingAnotherBullsHoleGivesItThatHole()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Pan;
            canvas.FitToView();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var state = window.Session.State;
            var first = state.Bulls.First(b => b.Scoring);
            var hole = state.Shots.First(s => s.IsShot && s.Bull is { } b && b != first.Index);

            // The bull's own center, where every one of its lines starts, nearer it than its hole.
            Click(window, canvas.ToControl(first.Image));
            Assert.Equal(first.Index, canvas.PickingBull);
            Assert.StartsWith($"Bull {first.Label} chosen.", window.BullLinesText[0], StringComparison.Ordinal);

            Click(window, canvas.ToControl(hole.Image));
            Assert.Equal(first.Index, window.Session.State.Find(hole.Id)!.Bull);
            Assert.True(window.Session.State.Find(hole.Id)!.BullChosen);
            Assert.Contains(window.BullLinesText, t => t.StartsWith(first.Label + " has", StringComparison.Ordinal));

            // A second click on the same hole takes it away again, and Escape ends the choosing.
            Click(window, canvas.ToControl(hole.Image));
            Assert.NotEqual(first.Index, window.Session.State.Find(hole.Id)!.Bull);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Null(canvas.PickingBull);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void TheLinesSwitchIsOnAtFirstAndRemembered()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            Assert.True(window.LinesShown);
            Assert.True(window.Canvas.LinesToBulls);
            window.LinesShown = false;
            Assert.False(window.Canvas.LinesToBulls);
            Assert.False(window.Settings.LoadLinesToBulls());
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void NextUnsureHoleSaysWhyOrThatThereAreNone()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            int unsure = BullLines.Unsure(window.Session.State).Count;
            Assert.Contains(BullChoosing.UnsureCount(unsure), window.BullLinesText);
            window.NextUnsureHole();
            if (unsure == 0)
            {
                Assert.Equal("No unsure holes.", window.StatusText);
            }
            else
            {
                Assert.NotNull(window.Canvas.Selected);
            }
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
