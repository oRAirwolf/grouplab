using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

using GroupLab.Tests.Support;

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

    /// <summary>Every stop Tab reaches in <paramref name="top"/>, round to the first again, and the ones a screen reader would find no name for.</summary>
    internal static (List<Control> Stops, List<string> Unnamed, bool Lost) TabRound(Window top)
    {
        top.Focus();
        var stops = new List<Control>();
        var unnamed = new List<string>();
        for (int i = 0; i < 800; i++)
        {
            Press(top, Key.Tab, PhysicalKey.Tab);
            if (top.FocusManager?.GetFocusedElement() is not Control c)
            {
                return (stops, unnamed, true);
            }

            if (stops.Contains(c))
            {
                break;
            }

            stops.Add(c);
            if (!ScreenReaderAudit.IsSpokenName(ControlAutomationPeer.CreatePeerForElement(c).GetName()))
            {
                unnamed.Add($"{c.GetType().Name} {AutomationProperties.GetAutomationId(c)} {c.Name}");
            }
        }

        return (stops, unnamed, false);
    }

    /// <summary>
    /// Entry 447 section 2: each screen tabbed all the way round: the focus never lost, never held inside one panel (the round reaches the
    /// bar down the left), and every stop named for Narrator and VoiceOver.
    /// </summary>
    [AvaloniaFact]
    public void EveryScreenTabsRoundWithEveryStopNamed()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.CalibreAnswered();
            // Entry 468 section 1: every page of Settings, each tabbed round and audited as a screen of its own.
            foreach (string place in new[] { "marking", "analysis", "Sessions", "Library", "Ballistics", "Compare", "Equipment" }.Concat(SettingsMenu.All.Select(c => "Settings, " + c)))
            {
                if (place == "analysis")
                {
                    window.Analyse();
                }
                else if (place.StartsWith("Settings, ", StringComparison.Ordinal))
                {
                    window.ShowSettings(Enum.Parse<SettingsCategory>(place["Settings, ".Length..]));
                }
                else if (place != "marking")
                {
                    window.Go(Enum.Parse<Destination>(place));
                }

                Dispatcher.UIThread.RunJobs();
                var (stops, unnamed, lost) = TabRound(window);
                Assert.False(lost, place + ": the focus was lost");
                Assert.True(stops.Count > 5 && stops.Count < 800, $"{place}: {stops.Count} stops");
                Assert.Contains(stops, s => s.Classes.Contains(GroupLab.App.Theme.AppStyles.RailButton));
                Assert.True(unnamed.Count == 0, place + ": " + string.Join("; ", unnamed));
                var found = ScreenReaderAudit.Findings(window, stops);
                Assert.True(found.Count == 0, place + ": " + string.Join("; ", found));

                // Entry 462 section 3: and laid out whole: nothing cut off, past the edge or drawn over, every line at AA contrast.
                var laid = LayoutAudit.Findings(window);
                Assert.True(laid.Count == 0, place + ": " + string.Join("; ", laid));
            }
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    /// <summary>Entry 447 section 2: the windows GroupLab opens over itself, the report and the printer check, tabbed round the same way.</summary>
    [AvaloniaFact]
    public void ItsOwnWindowsTabRoundWithEveryStopNamed()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.OpenPrinterCheck();
            window.ShowSettings(SettingsCategory.Help);
            Dispatcher.UIThread.RunJobs();
            window.GetVisualDescendants().OfType<Button>().First(b => b.Content is string words && words.StartsWith("Report a problem", StringComparison.Ordinal))
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.OwnedWindows.Count >= 2, "the printer check and the report did not both open");
            foreach (var owned in window.OwnedWindows.ToList())
            {
                var (stops, unnamed, lost) = TabRound(owned);
                Assert.False(lost, owned.Title + ": the focus was lost");
                Assert.True(stops.Count > 1, owned.Title + ": nothing takes the focus");
                Assert.True(unnamed.Count == 0, owned.Title + ": " + string.Join("; ", unnamed));
                var found = ScreenReaderAudit.Findings(owned, stops);
                Assert.True(found.Count == 0, owned.Title + ": " + string.Join("; ", found));
                var laid = LayoutAudit.Findings(owned);
                Assert.True(laid.Count == 0, owned.Title + ": " + string.Join("; ", laid));
                owned.Close();
            }
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
