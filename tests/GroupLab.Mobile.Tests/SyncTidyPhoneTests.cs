using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 464 section 1 on the phone: the Sync page says in one line what tidying old change files moved to the folder's
/// trash, laid out whole at a phone's width, and nothing before there was any.
/// </summary>
public sealed class SyncTidyPhoneTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-464p-{Guid.NewGuid():N}");

    public SyncTidyPhoneTests()
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
        PhoneSync.Save(PhoneSync.Setup with { TidiedUtc = null, TidiedFiles = 0, TidiedBytes = 0, FreedBytes = 0 });
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

    private static SessionRecord Session(string created) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        "{\"marks\":1}", 25, 0.3, 0.25, 0.4, null, new string('b', 64), [1, 2, 3], "image/jpeg");

    [AvaloniaFact]
    public void TheSyncPageSaysWhatTidyingMovedToTheTrash()
    {
        var mine = PhoneAnalysis.Store();
        foreach (var s in mine.List())
        {
            mine.Delete(s.Id);
        }

        string desk = Path.Combine(root, "desk");
        Directory.CreateDirectory(desk);
        var other = SessionStore.Open(Path.Combine(desk, "grouplab.db"));
        var otherLocal = new FileSyncLocal(Path.Combine(desk, "sheets"), Path.Combine(desk, "settings.json"));
        TestPhone.SyncFolderToPick = Path.Combine(root, "cloud");
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            long id = mine.Save(Session("2026-10-21T09:00:00Z"));
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());
            for (int edit = 2; edit <= 3; edit++)
            {
                mine.Save(mine.Get(id)! with { MarkingJson = $"{{\"marks\":{edit}}}" });
                Wait(PhoneSync.Now());
            }

            SyncEngine.Run(other, otherLocal, new DirectorySyncFolder(Path.Combine(root, "cloud")), "Desk", DateTime.UtcNow);
            shell.ShowSync();
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(Words(window), w => w.StartsWith("Tidied", StringComparison.Ordinal));

            // Tidying runs once a day; the last one is set a day back so this run tidies.
            mine.MarkSyncTidied("2026-01-01T00:00:00.000Z");
            Wait(PhoneSync.Now());
            window.UpdateLayout();
            Assert.True(PhoneSync.Setup.TidiedFiles >= 1);
            Assert.Contains(Words(window), w => w.StartsWith("Tidied just now: ", StringComparison.Ordinal)
                && w.Contains("moved to the sync folder's trash for 14 days", StringComparison.Ordinal));
            var laid = LayoutAudit.Findings(window);
            Assert.True(laid.Count == 0, string.Join("; ", laid));
        }
        finally
        {
            window.Close();
            foreach (var s in mine.List())
            {
                mine.Delete(s.Id);
            }
        }
    }
}
