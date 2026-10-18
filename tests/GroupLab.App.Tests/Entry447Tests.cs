using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 447 section 1: on the marking picture, with a hole selected, an arrow nudges it one pixel of the picture
/// and Shift and an arrow ten, one Undo step for a run of presses; with nothing selected the arrows pan the view; never while a text or
/// number box has the focus.
/// </summary>
public class Entry447Tests
{
    private static void Press(Window window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        window.KeyRelease(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheArrowsNudgeTheSelectedHoleAndOneUndoTakesBackTheRun()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            var shot = window.Session.State.Shots.First(s => s.IsShot);
            var start = shot.Image;
            window.Canvas.Selected = shot.Id;
            window.Canvas.Focus();
            Dispatcher.UIThread.RunJobs();

            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Assert.Equal(new PointD(start.X + 3, start.Y), window.Session.State.Find(shot.Id)!.Image);
            Press(window, Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Shift);
            Assert.Equal(new PointD(start.X + 3, start.Y + 10), window.Session.State.Find(shot.Id)!.Image);
            Assert.Same(window.Canvas, window.FocusManager?.GetFocusedElement());

            window.Session.Undo();
            Assert.Equal(start, window.Session.State.Find(shot.Id)!.Image);
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void WithNothingSelectedTheArrowsPanTheView()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.Canvas.Selected = null;
            window.Canvas.Focus();
            Dispatcher.UIThread.RunJobs();
            var before = window.Canvas.View.Offset;
            var shots = window.Session.State.Shots.Select(s => s.Image).ToList();
            Press(window, Key.Right, PhysicalKey.ArrowRight);
            Assert.Equal(before.X - 40, window.Canvas.View.Offset.X, 3);
            Press(window, Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Shift);
            Assert.Equal(before.Y + 200, window.Canvas.View.Offset.Y, 3);
            Assert.Equal(shots, window.Session.State.Shots.Select(s => s.Image).ToList());
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void NeverWhileATextBoxHasTheFocus()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            var shot = window.Session.State.Shots.First(s => s.IsShot);
            window.Canvas.Selected = shot.Id;
            var box = window.GetVisualDescendants().OfType<TextBox>().First(b => b.IsEffectivelyVisible && b.IsEffectivelyEnabled);
            box.Focus();
            Dispatcher.UIThread.RunJobs();
            Press(window, Key.Left, PhysicalKey.ArrowLeft);
            Assert.Equal(shot.Image, window.Session.State.Find(shot.Id)!.Image);
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [Fact]
    public void ARunIsOneUndoStepAndANewRunIsAnother()
    {
        var session = new MarkingSession(MarkingState.Empty);
        int id = session.AddShot(new PointD(10, 10));
        session.NudgeShot(id, new PointD(1, 0), joinRun: false);
        session.NudgeShot(id, new PointD(1, 0), joinRun: true);
        session.NudgeShot(id, new PointD(0, 1), joinRun: false);
        Assert.Equal(new PointD(12, 11), session.State.Find(id)!.Image);
        session.Undo();
        Assert.Equal(new PointD(12, 10), session.State.Find(id)!.Image);
        session.Undo();
        Assert.Equal(new PointD(10, 10), session.State.Find(id)!.Image);
    }
}
