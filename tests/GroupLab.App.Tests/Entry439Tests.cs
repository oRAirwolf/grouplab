using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 439: the guided demo. The whole tour is walked headless, and fails when a step names a control that is not
/// on its screen, so a renamed button breaks this test rather than the tour; and nothing the demo makes is kept unless the person asks.
/// </summary>
public class Entry439Tests
{
    private static (MainWindow Window, string Folder) Fresh()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry439-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, folder);
    }

    /// <summary>Runs the window's dispatcher until the task finishes, a minute at most.</summary>
    private static void Wait(Task task)
    {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (!task.IsCompleted && DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(task.IsCompleted, "the demo did not finish its step within a minute");
        task.GetAwaiter().GetResult();
    }

    private static List<long> Sessions(MainWindow window) => [.. window.Sessions!.List().Select(s => s.Id).OrderBy(id => id)];

    [AvaloniaFact]
    public void EveryStepPointsAtAControlOnItsScreenAndAFinishedTourLeavesNothing()
    {
        var (window, folder) = Fresh();
        try
        {
            var before = Sessions(window);
            Wait(window.StartDemo());
            Assert.True(window.DemoRunning);
            Assert.True(window.Session.State.Shots.Count >= 20, $"the demo sheet was read with {window.Session.State.Shots.Count} shots: {window.StatusText}");
            for (int i = 0; i < MainWindow.DemoSteps.Count; i++)
            {
                var step = MainWindow.DemoSteps[i];
                Assert.Equal(i, window.DemoAt);
                Dispatcher.UIThread.RunJobs();
                Assert.True(window.DemoSpotlit is not null, $"step {i + 1}, {step.Title}, points at {step.Control!}, which is not on the screen (analysing {window.Analysing}: {window.StatusText})");
                Assert.True(window.DemoSpotlit!.IsEffectivelyVisible, $"step {i + 1} points at {step.Control}, which is hidden");
                Wait(window.DemoNext());
            }

            Assert.False(window.DemoRunning);
            Assert.Equal(before, Sessions(window));
            Assert.DoesNotContain(window.Targets, t => t.Demo);
            Assert.False(Directory.Exists(Path.Combine(folder, "demo")) && Directory.EnumerateFiles(Path.Combine(folder, "demo")).Any(),
                "the demo's picture was left behind");
        }
        finally
        {
            window.Close();
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void ATourLeftPartWayLeavesNothingAndCanCarryOnFromThatStep()
    {
        var (window, folder) = Fresh();
        try
        {
            var before = Sessions(window);
            Wait(window.StartDemo());
            for (int i = 0; i < 6; i++)
            {
                Wait(window.DemoNext());
            }

            // The analysis was saved as a session by step 5; Escape ends the tour and takes it away again.
            Assert.NotEqual(before, Sessions(window));
            window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Dispatcher.UIThread.RunJobs();
            Assert.False(window.DemoRunning);
            Assert.Equal(before, Sessions(window));

            Wait(window.StartDemo(6));
            Assert.Equal(6, window.DemoAt);
            Assert.NotNull(window.DemoSpotlit);
            window.EndDemo(keep: false);
            Assert.Equal(before, Sessions(window));
        }
        finally
        {
            window.Close();
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void KeepingTheDemoKeepsItsSession()
    {
        var (window, folder) = Fresh();
        try
        {
            var before = Sessions(window);
            Wait(window.StartDemo(MainWindow.DemoSteps.Count - 1));
            window.DemoKeep.IsChecked = true;
            Wait(window.DemoNext());
            Assert.False(window.DemoRunning);
            Assert.Single(Sessions(window).Except(before));
        }
        finally
        {
            window.Close();
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void TheDemoIsOfferedOnceWhateverTheAnswer()
    {
        var (window, folder) = Fresh();
        try
        {
            window.OfferDemoIfDue();
            Assert.True(window.DemoOfferShown);
            window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Assert.False(window.DemoOfferShown);
            window.OfferDemoIfDue();
            Assert.False(window.DemoOfferShown);
        }
        finally
        {
            window.Close();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ThePrintStepNeverPrints()
    {
        // Entry 439 section 2.11: the tour stops at the Print button; printing is the person's own press.
        var print = MainWindow.DemoSteps.Single(s => s.Control == "Print…");
        Assert.Null(print.Do);
    }
}
