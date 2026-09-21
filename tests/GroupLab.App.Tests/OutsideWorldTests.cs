using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 122 section 3: the buttons that would reach out of the process are clicked against the recorder, so they are
/// measured rather than excluded by name, and what each asked for is checked.
/// <para>
/// The recorder is in place for the whole run, put there by the module initialiser, so no test anywhere can open anything on the machine it
/// runs on. That is the fix for browser tabs appearing while the tests ran.
/// </para>
/// </summary>
public class OutsideWorldTests
{
    private static MainWindow Settings()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Click(MainWindow window, string label)
    {
        var button = window.GetLogicalDescendants().OfType<Button>().First(b => Equals(b.Content, label));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheApplicationIsTheOnlyThingThatOpensAnythingOutsideItself()
    {
        // The real one is never in place during a test run. If this fails, a test opened something on somebody's machine.
        Assert.IsType<RecordedOutsideWorld>(TheOutsideWorld.Current);
        Assert.Same(TestDefaults.Outside, TheOutsideWorld.Current);
    }

    [AvaloniaFact]
    public void TheLinkToTheProjectAsksForTheRepositoryAndOpensNothing()
    {
        var window = Settings();
        int before = TestDefaults.Outside.Asked.Count;
        Click(window, "The project on GitHub");

        var asked = TestDefaults.Outside.Asked.Skip(before).ToList();
        var one = Assert.Single(asked);
        Assert.Equal("address", one.What);
        Assert.Equal("https://github.com/oRAirwolf/grouplab", one.Target);
        window.Close();
    }

    [AvaloniaFact]
    public void TheSupportPlaceholderOpensNothingAtAll()
    {
        var window = Settings();
        int before = TestDefaults.Outside.Asked.Count;
        Click(window, SupportLink.Label);

        // Entry 120 section 9: there is no address, so the item says so and asks for nothing.
        Assert.Empty(TestDefaults.Outside.Asked.Skip(before));
        Assert.Equal(SupportLink.NoAddressYet, window.StatusText);
        window.Close();
    }

    [AvaloniaFact]
    public void CheckNowReachesNothingWhileThereIsNoKey()
    {
        var window = Settings();
        int before = TestDefaults.Outside.Asked.Count;
        window.CheckForUpdates();
        Dispatcher.UIThread.RunJobs();

        // A build with no key compiled in refuses every update, so it does not even ask.
        Assert.Empty(TestDefaults.Outside.Asked.Skip(before));
        window.Close();
    }
}
