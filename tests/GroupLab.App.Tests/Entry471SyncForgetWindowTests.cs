using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 471 section 2, question 109 (a) on the computer: each other device on the Sync section has "Forget this
/// device", which asks once on the page, saying nothing in the folder is deleted, and "Leave it as it is" forgets nothing.
/// </summary>
public sealed class Entry471SyncForgetWindowTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-471-{Guid.NewGuid():N}");

    public Entry471SyncForgetWindowTests() => Directory.CreateDirectory(Path.Combine(root, "cloud"));

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private string Cloud => Path.Combine(root, "cloud");

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
        Assert.True(task.IsCompleted, "the sync did not finish");
    }

    private static void Press(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ForgetThisDeviceAsksFirstAndThenTheDeviceIsGoneFromTheList()
    {
        var settings = new AppSettingsStore(Path.Combine(root, "desk.json"));
        settings.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(settings) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        string laptop = Path.Combine(root, "laptop");
        Directory.CreateDirectory(laptop);
        var other = SessionStore.Open(Path.Combine(laptop, "grouplab.db"));
        try
        {
            SyncEngine.Run(other, new FileSyncLocal(Path.Combine(laptop, "sheets"), Path.Combine(laptop, "settings.json")), new DirectorySyncFolder(Cloud), "Range laptop", DateTime.UtcNow);
            window.ChooseSyncFolder(Cloud);
            Wait(window.SyncNowAsync());
            Assert.Contains(window.SyncText, t => t.StartsWith("Range laptop", StringComparison.Ordinal));

            Button Forget() => window.SyncButtons.Single(b => AutomationProperties.GetName(b) == SyncScreens.ForgetDevice + ": Range laptop");
            Button Choice(string words) => window.SyncButtons.Single(b => b.Content as string == words && AutomationProperties.GetName(b) is null or "");

            // Asked first, saying nothing in the folder is deleted; Leave it as it is forgets nothing.
            bool Asking() => ((Control)Choice(SyncScreens.LeaveIt).Parent!.Parent!).IsVisible;
            Assert.False(Asking());
            Press(Forget());
            Assert.True(Asking());
            Assert.Contains(window.SyncText, t => t == SyncScreens.ForgetAsks("Range laptop"));
            Assert.Contains("Nothing in the folder is deleted", SyncScreens.ForgetAsks("Range laptop"), StringComparison.Ordinal);
            Press(Choice(SyncScreens.LeaveIt));
            Assert.False(Asking());
            Assert.False(SyncForget.IsForgotten(new DirectorySyncFolder(Cloud), other.SyncDevice()));

            Press(Forget());
            Press(Choice(SyncScreens.ForgetDevice));
            Assert.True(SyncForget.IsForgotten(new DirectorySyncFolder(Cloud), other.SyncDevice()));
            Assert.DoesNotContain(window.SyncText, t => t.StartsWith("Range laptop", StringComparison.Ordinal));
            Assert.Contains(SyncScreens.NoOtherDevices, window.SyncText);
        }
        finally
        {
            window.Close();
        }
    }
}
