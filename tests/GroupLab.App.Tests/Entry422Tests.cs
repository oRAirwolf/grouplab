using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 422 (Jylee): a click on the picture lands the side panel on the section it changed, with the heading and the
/// chosen row fully in view, flashes it (or holds a tint where the computer asks for less motion), and says what it did on the status line.
/// </summary>
public class Entry422Tests
{
    private static void Settle()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Click(MainWindow window, Point onCanvas, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var at = window.Canvas.TranslatePoint(onCanvas, window)!.Value;
        window.MouseDown(at, MouseButton.Left, modifiers);
        window.MouseUp(at, MouseButton.Left, modifiers);
        Settle();
    }

    /// <summary>Whether a control is wholly inside the side panel's visible part.</summary>
    private static void AssertInView(MainWindow window, Control control, string what)
    {
        var scroll = window.SideScroll;
        var presenter = scroll.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().First();
        var top = control.TranslatePoint(default, presenter)!.Value.Y;
        double bottom = top + control.Bounds.Height;
        Assert.True(top >= -0.5 && bottom <= scroll.Viewport.Height + 0.5,
            $"{what} is from {top:0} to {bottom:0} and the side panel shows 0 to {scroll.Viewport.Height:0} (offset {scroll.Offset.Y:0} of {scroll.Extent.Height:0}, presenter {presenter.Bounds.Height:0}).");
    }

    private static void AssertLanded(MainWindow window, string what)
    {
        var (heading, row, rowEnd) = window.LandedOn!.Value;
        AssertInView(window, row, what + "'s row");
        AssertInView(window, rowEnd, what + "'s row end");
        // The heading shows too, unless the section is taller than the panel, when the row wins.
        double sectionToRow = rowEnd.TranslatePoint(new Point(0, rowEnd.Bounds.Height), heading)!.Value.Y;
        if (sectionToRow + Landing.Margin <= window.SideScroll.Viewport.Height)
        {
            AssertInView(window, heading, what + "'s heading");
        }
    }

    [Theory]
    [InlineData(0, 900, 3000, 1500, 1520, 1492.0)]
    [InlineData(1492, 900, 3000, 1500, 1520, null)]
    [InlineData(0, 400, 2000, 100, 1100, 708.0)]
    [InlineData(0, 400, 2000, 1900, 1990, 1600.0)]
    public void TheHeadingLandsAMarginBelowTheTopAndATallSectionGivesWayToItsRow(double offset, double viewport, double extent, double sectionTop,
        double rowBottom, double? expected)
    {
        // A section taller than the column: the row wins (row 3); the last section: as far as the column goes (row 4).
        double rowTop = Math.Max(sectionTop, rowBottom - 20);
        Assert.Equal(expected, Landing.Target(offset, viewport, extent, (sectionTop, rowBottom), (rowTop, rowBottom)));
    }

    [AvaloniaTheory]
    [InlineData(900)]
    [InlineData(700)]
    [InlineData(520)]
    public void AClickOnAHoleLandsOnItsOpenRowUnderShotsAndSaysSo(int height)
    {
        var (window, path, _) = Entry109Tests.Sheet(1400, height);
        var was = Landing.ReducedMotion;
        try
        {
            Landing.ReducedMotion = () => false;
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Pan;
            canvas.FitToView();
            Settle();
            var state = window.Session.State;
            // The last hole listed, whose row is at the bottom of the panel, the section with nothing below it to scroll into.
            int last = ShotList.Rows(state).Last().ShotId;
            Click(window, canvas.ToControl(state.Find(last)!.Image));
            Assert.Equal(last, canvas.Selected);
            AssertLanded(window, "the hole");
            Assert.EndsWith("selected; its row is open under Shots.", window.StatusText, StringComparison.Ordinal);
            Assert.True(window.SideLanding!.Last.Moved);
            Assert.True(window.SideLanding.Last.Flashed);

            // A click on another hole whose row then fits where the panel already is still lands it; Next unsure hole does the same.
            int first = ShotList.Rows(state).First().ShotId;
            Click(window, canvas.ToControl(state.Find(first)!.Image));
            AssertLanded(window, "the first hole");
            window.NextUnsureHole();
            Settle();
            if (canvas.Selected is not null)
            {
                AssertLanded(window, "the next unsure hole");
            }
        }
        finally
        {
            Landing.ReducedMotion = was;
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaTheory]
    [InlineData(900)]
    [InlineData(700)]
    [InlineData(520)]
    public void AClickOnABullLandsOnWhichBullEachShotBelongsToAndSaysWhatComesNext(int height)
    {
        var (window, path, _) = Entry109Tests.Sheet(1400, height);
        try
        {
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Pan;
            canvas.FitToView();
            Settle();
            // Scrolled to the bottom first, so the bull's section is out of view and the click has to move the panel.
            window.SideScroll.Offset = new Vector(0, window.SideScroll.Extent.Height);
            Settle();
            var bull = window.Session.State.Bulls.First(b => b.Scoring);
            Click(window, canvas.ToControl(bull.Image));
            Assert.Equal(bull.Index, canvas.PickingBull);
            AssertLanded(window, "the bull");
            Assert.Equal(ClickWords.BullChosen(bull.Label), window.StatusText);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaTheory]
    [InlineData(900)]
    [InlineData(700)]
    [InlineData(520)]
    public void ShiftOnABullLandsOnLoadsOnThisSheet(int height)
    {
        var (window, path, _) = Entry109Tests.Sheet(1400, height);
        try
        {
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Select;
            canvas.FitToView();
            Settle();
            window.SideScroll.Offset = new Vector(0, 0);
            Settle();
            var bull = window.Session.State.Bulls.First(b => b.Scoring);
            Click(window, canvas.ToControl(bull.Image), RawInputModifiers.Shift);
            AssertLanded(window, "the load's bulls");
            Assert.Equal(ClickWords.BullForLoad(bull.Label, true, 1), window.StatusText);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }

    [AvaloniaFact]
    public void WithLessMotionTheSectionHoldsATintAndAClickThatDoesNotMoveFlashesNothing()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        var was = Landing.ReducedMotion;
        try
        {
            Landing.ReducedMotion = () => true;
            var canvas = window.Canvas;
            canvas.Tool = MarkingTool.Pan;
            canvas.FitToView();
            Settle();
            var state = window.Session.State;
            int last = ShotList.Rows(state).Last().ShotId;
            Click(window, canvas.ToControl(state.Find(last)!.Image));
            Assert.Equal((true, false, true), window.SideLanding!.Last);

            // The same hole again: the panel is where it should be already, so nothing moves and nothing flashes.
            Click(window, canvas.ToControl(state.Find(last)!.Image));
            Assert.Equal((false, false, false), window.SideLanding.Last);
        }
        finally
        {
            Landing.ReducedMotion = was;
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
