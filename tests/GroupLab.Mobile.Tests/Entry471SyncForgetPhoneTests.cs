using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 471 section 2, question 109 (a) on the phone: each other device on the Sync page has "Forget this device",
/// which asks once on the page, saying nothing in the folder is deleted, and then the device is gone from the list; laid out whole at a
/// phone's width while it asks.
/// </summary>
public sealed class Entry471SyncForgetPhoneTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-471p-{Guid.NewGuid():N}");

    public Entry471SyncForgetPhoneTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
    }

    public void Dispose()
    {
        PhoneSync.Stop();
        TestPhone.SyncFolderToPick = null;
        Temp.Delete(root);
    }

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

    private static IEnumerable<string> Words(Control c) => c.GetLogicalDescendants()
        .Select(d => d switch { TextBlock t => t.Text, ContentControl { Content: string s } => s, _ => null }).OfType<string>();

    private static Button Find(Window window, string id) =>
        window.GetLogicalDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == id);

    private static void Press(Window window, string id)
    {
        Find(window, id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    [AvaloniaFact]
    public void ForgetThisDeviceAsksFirstAndThenTheDeviceIsGoneFromTheList()
    {
        string desk = Path.Combine(root, "desk");
        Directory.CreateDirectory(desk);
        var other = SessionStore.Open(Path.Combine(desk, "grouplab.db"));
        var cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
        SyncEngine.Run(other, new FileSyncLocal(Path.Combine(desk, "sheets"), Path.Combine(desk, "settings.json")), cloud, "Desk", DateTime.UtcNow);
        TestPhone.SyncFolderToPick = Path.Combine(root, "cloud");
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());
            shell.ShowSync();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(Words(window), w => w.StartsWith("Desk", StringComparison.Ordinal));
            Assert.Equal(SyncScreens.ForgetDevice + ": Desk", AutomationProperties.GetName(Find(window, "sync-forget")));

            Press(window, "sync-forget");
            Assert.Contains(SyncScreens.ForgetAsks("Desk"), Words(window));
            var laid = LayoutAudit.Findings(window);
            Assert.True(laid.Count == 0, string.Join("; ", laid));
            Assert.False(SyncForget.IsForgotten(cloud, other.SyncDevice()));

            Press(window, "sync-forget-yes");
            Assert.True(SyncForget.IsForgotten(cloud, other.SyncDevice()));
            Assert.DoesNotContain(Words(window), w => w.StartsWith("Desk", StringComparison.Ordinal));
            Assert.Contains(SyncScreens.NoOtherDevices, Words(window));
        }
        finally
        {
            window.Close();
        }
    }
}
