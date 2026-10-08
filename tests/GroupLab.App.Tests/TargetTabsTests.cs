using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// Several targets open at once, concept A as Alan chose it (planning, 2026-10-07): tabs across the top, each with its name, its state, a
/// saved or unsaved dot and a close button that asks first when it has unsaved marks; a "+" tab; Ctrl+Tab and Ctrl+Shift+Tab. Each tab keeps
/// its own target, zoom and unsaved marks, and switching never loses or mixes anything.
/// </summary>
public class TargetTabsTests
{
    [Fact]
    public void TheWindowStartsWithOneEmptyTab()
    {
        var targets = new OpenTargets();
        Assert.Single(targets.All);
        Assert.Same(targets.All[0], targets.Current);
        Assert.Equal("New target", targets.Current.Label);
        Assert.Equal("Marking", targets.Current.State);
        Assert.False(OpenTargets.CloseAsks(targets.Current));
    }

    [Fact]
    public void TheStateSaysAnalysisOrMarkingAndWhetherItIsSaved()
    {
        Assert.Equal("Analysis", TargetTabWords.State(analysing: true, unsaved: false));
        Assert.Equal("Marking", TargetTabWords.State(analysing: false, unsaved: false));
        Assert.Equal("Marking, not saved", TargetTabWords.State(analysing: false, unsaved: true));
        Assert.Equal("Analysis, not saved", TargetTabWords.State(analysing: true, unsaved: true));
        Assert.Equal("New target", TargetTabWords.Label(null));
        Assert.Equal("New target", TargetTabWords.Label("  "));
        Assert.Equal("range-day.jpg", TargetTabWords.Label("range-day.jpg"));
        var tab = new OpenTargets().Current;
        tab.Name = "range-day.jpg";
        tab.Unsaved = true;
        Assert.Equal("range-day.jpg, Marking, not saved", tab.Spoken);
        Assert.True(OpenTargets.CloseAsks(tab));
    }

    [Fact]
    public void AddingPutsTheTabAtTheEndAndCtrlTabGoesRoundBothWays()
    {
        var targets = new OpenTargets();
        var first = targets.Current;
        var second = targets.Add();
        var third = targets.Add();
        Assert.Equal([first, second, third], targets.All);

        // Added, not yet shown: the "+" tab shows it itself.
        Assert.Same(first, targets.Current);
        Assert.Same(second, targets.Next());
        Assert.Same(third, targets.Next(backwards: true));

        targets.Show(third);
        Assert.Equal(2, targets.CurrentIndex);
        Assert.Same(first, targets.Next());
        Assert.Same(second, targets.Next(backwards: true));
        Assert.NotEqual(first.Number, second.Number);
    }

    [Fact]
    public void ClosingShowsTheNeighbourAndTheLastTabLeavesAnEmptyOne()
    {
        var targets = new OpenTargets();
        var a = targets.Current;
        var b = targets.Add();
        var c = targets.Add();

        // Closing a tab that is not showing leaves the one showing where it is.
        Assert.Same(a, targets.Close(c));
        Assert.Equal([a, b], targets.All);

        // Closing the one showing shows its right-hand neighbour, or its left where it was the last.
        Assert.Same(b, targets.Close(a));
        var d = targets.Add();
        targets.Show(d);
        Assert.Same(b, targets.Close(d));

        // The only tab closed leaves a new, empty one, so there is always somewhere to open a picture.
        var fresh = targets.Close(b);
        Assert.Single(targets.All);
        Assert.Same(fresh, targets.Current);
        Assert.Equal("New target", fresh.Label);
        Assert.Throws<ArgumentException>(() => targets.Close(b));
    }

    private static MainWindow NewWindow(bool saveByHand = false)
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        store.SaveSaveByHand(saveByHand);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    private static string Picture(string name)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}.png");
        using var image = new Mat(300, 400, MatType.CV_8UC3, new Scalar(235, 235, 235));
        Cv2.Circle(image, new OpenCvSharp.Point(200, 150), 9, new Scalar(30, 30, 30), -1);
        Cv2.ImWrite(path, image);
        return path;
    }

    private static void Mark(MainWindow window, int shots)
    {
        var session = window.Session;
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(200, 150));
        for (int k = 0; k < shots; k++)
        {
            double angle = 2 * Math.PI * k / shots;
            session.AddShot(new PointD(200 + (30 * Math.Cos(angle)), 150 + (30 * Math.Sin(angle))));
        }

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void EachTabKeepsItsOwnTargetZoomAndUndo()
    {
        var window = NewWindow(saveByHand: true);
        window.Show();
        string first = Picture("first"), second = Picture("second");
        window.OpenImage(first);
        Mark(window, 5);
        window.Canvas.ZoomBy(2.5);
        var firstView = window.Canvas.View;
        var firstSession = window.Session;
        Assert.Equal([(Path.GetFileName(first), "Marking, not saved", "Not saved", true)], window.TabsShown);

        // The "+" tab: a new, empty target, shown, with the first parked beside it.
        Assert.NotNull(window.AddTarget(choose: false));
        Assert.Equal(2, window.Targets.Count);
        Assert.Same(window.Targets[1], window.CurrentTarget);
        Assert.NotSame(firstSession, window.Session);
        Assert.Empty(window.Session.State.Shots);
        Assert.Null(window.Session.State.ImagePath);
        Assert.Equal(MainWindow.NewTargetStatus, window.StatusText);
        Assert.Equal(("New target", "Marking", "Saved", true), window.TabsShown[1]);
        Assert.Equal((Path.GetFileName(first), "Marking, not saved", "Not saved", false), window.TabsShown[0]);

        window.OpenImage(second);
        Mark(window, 3);
        Assert.Equal(3, window.Session.State.Shots.Count);

        // Back to the first: its marks, its picture, its zoom and the place it was left, and its own undo.
        Assert.True(window.ShowTarget(0));
        Assert.Same(firstSession, window.Session);
        Assert.Equal(5, window.Session.State.Shots.Count);
        Assert.Equal(first, window.Session.State.ImagePath);
        Assert.Equal(firstView, window.Canvas.View);
        Assert.Equal(400, window.Canvas.Frame.Width);
        Assert.True(window.HasUnsavedWork);
        window.Session.Undo();
        Assert.Equal(4, window.Session.State.Shots.Count);

        // And the second is as it was left, untouched by the undo on the first.
        Assert.True(window.ShowTarget(1));
        Assert.Equal(3, window.Session.State.Shots.Count);
        Assert.Equal(second, window.Session.State.ImagePath);
        Assert.Equal((Path.GetFileName(first), "Marking, not saved", "Not saved", false), window.TabsShown[0]);
        window.Close();
    }

    [AvaloniaFact]
    public void CtrlTabGoesToTheNextTabAndCtrlShiftTabToTheOneBefore()
    {
        var window = NewWindow();
        window.Show();
        window.AddTarget(choose: false);
        window.AddTarget(choose: false);
        Assert.Same(window.Targets[2], window.CurrentTarget);

        window.KeyPress(Key.Tab, RawInputModifiers.Control, PhysicalKey.Tab, null);
        Assert.Same(window.Targets[0], window.CurrentTarget);
        window.KeyPress(Key.Tab, RawInputModifiers.Control, PhysicalKey.Tab, null);
        Assert.Same(window.Targets[1], window.CurrentTarget);
        window.KeyPress(Key.Tab, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.Tab, null);
        window.KeyPress(Key.Tab, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.Tab, null);
        Assert.Same(window.Targets[2], window.CurrentTarget);
        Assert.Equal([false, false, true], window.TabsShown.Select(t => t.Chosen));
        window.Close();
    }

    [AvaloniaFact]
    public void ClosingATabWithUnsavedMarksAsksFirst()
    {
        var window = NewWindow(saveByHand: true);
        window.Show();
        window.OpenImage(Picture("asks"));
        Mark(window, 5);
        window.AddTarget(choose: false);

        // A tab with nothing unsaved closes at once, and the one showing stays.
        var empty = window.AddTarget(choose: false)!;
        window.ShowTarget(1);
        window.CloseTarget(empty);
        Assert.Equal(2, window.Targets.Count);
        Assert.Same(window.Targets[1], window.CurrentTarget);

        // The unsaved one is shown and asks; cancel keeps every mark.
        window.CloseTarget(0);
        Assert.Same(window.Targets[0], window.CurrentTarget);
        Assert.True(window.AskingAboutUnsavedWork);
        window.AnswerCancel();
        Assert.Equal(2, window.Targets.Count);
        Assert.Equal(5, window.Session.State.Shots.Count);

        // Asked again and discarded, it goes, and the tab beside it shows.
        window.CloseTarget(0);
        window.AnswerDiscard();
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.Targets);
        Assert.Empty(window.Session.State.Shots);
        Assert.Equal(("New target", "Marking", "Saved", true), window.TabsShown[0]);
        window.Close();
    }

    [AvaloniaFact]
    public void UnderSavingByItselfATabIsSavedAsItIsLeft()
    {
        var window = NewWindow();
        window.Show();
        window.OpenImage(Picture("autosave"));
        Mark(window, 4);
        Assert.True(window.SaveIsWaiting);

        window.AddTarget(choose: false);
        Assert.Equal((window.TabsShown[0].Name, "Marking", "Saved", false), window.TabsShown[0]);
        window.ShowTarget(0);
        Assert.NotNull(window.CurrentSession);
        Assert.False(window.HasUnsavedWork);
        Assert.Equal(4, window.Session.State.Shots.Count);
        window.Close();
    }

    [AvaloniaFact]
    public void ClosingTheWindowAsksAboutEveryTabWithUnsavedMarks()
    {
        var window = NewWindow(saveByHand: true);
        window.Show();
        window.OpenImage(Picture("closing"));
        Mark(window, 5);
        window.AddTarget(choose: false);

        // The window stays open and the tab with unsaved marks comes forward with the question.
        window.Close();
        Assert.True(window.IsVisible);
        Assert.Same(window.Targets[0], window.CurrentTarget);
        Assert.True(window.AskingAboutUnsavedWork);

        // Answered, the window carries on closing.
        window.AnswerDiscard();
        Dispatcher.UIThread.RunJobs();
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void TheTabRowHasThePlusTabAndACloseButtonOnEveryTab()
    {
        var window = NewWindow();
        window.Show();
        window.AddTarget(choose: false);
        var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();
        Assert.Single(buttons, b => Avalonia.Automation.AutomationProperties.GetName(b) == "Open another target");
        Assert.Equal(2, buttons.Count(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Close this target"));

        // The close button on the first tab, pressed as a person would.
        buttons.First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Close this target").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Single(window.Targets);
        window.Close();
    }
}
