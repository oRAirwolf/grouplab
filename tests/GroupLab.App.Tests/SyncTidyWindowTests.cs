using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 464 section 1 on the computer: once another device has read an old change file and a later one carries a
/// newer version of all it holds, the Sync section says in one line what tidying moved to the folder's trash, and says nothing before.
/// </summary>
public sealed class SyncTidyWindowTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-464-{Guid.NewGuid():N}");

    public SyncTidyWindowTests() => Directory.CreateDirectory(Path.Combine(root, "cloud"));

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

    private static SessionRecord Session(string created) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        "{\"marks\":1}", 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    [AvaloniaFact]
    public void TheSyncSectionSaysWhatTidyingMovedToTheTrash()
    {
        var settings = new AppSettingsStore(Path.Combine(root, "desk.json"));
        settings.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(settings) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        string laptop = Path.Combine(root, "laptop");
        Directory.CreateDirectory(laptop);
        var other = SessionStore.Open(Path.Combine(laptop, "grouplab.db"));
        var otherLocal = new FileSyncLocal(Path.Combine(laptop, "sheets"), Path.Combine(laptop, "settings.json"));
        try
        {
            var mine = window.Sessions!;
            mine.Save(Session("2026-10-20T10:00:00Z"));
            window.ChooseSyncFolder(Cloud);
            Wait(window.SyncNowAsync());
            for (int edit = 2; edit <= 3; edit++)
            {
                mine.Save(mine.Get(mine.List().Single().Id)! with { MarkingJson = $"{{\"marks\":{edit}}}" });
                Wait(window.SyncNowAsync());
            }

            SyncEngine.Run(other, otherLocal, new DirectorySyncFolder(Cloud), "Range laptop", DateTime.UtcNow);
            Assert.DoesNotContain(window.SyncText, t => t.StartsWith("Tidied", StringComparison.Ordinal));

            // Tidying runs once a day; the last one is set a day back so this run tidies.
            mine.MarkSyncTidied("2026-01-01T00:00:00.000Z");
            Wait(window.SyncNowAsync());
            Assert.True(window.Sync.TidiedFiles >= 1);
            Assert.Contains(window.SyncText, t => t.StartsWith("Tidied just now: ", StringComparison.Ordinal)
                && t.Contains("moved to the sync folder's trash for 14 days", StringComparison.Ordinal));
            Assert.NotEmpty(Directory.GetFiles(Path.Combine(Cloud, SyncEngine.Root, "trash", mine.SyncDevice())));
            Assert.Equal("{\"marks\":3}", other.Get(other.List().Single().Id)!.MarkingJson);
        }
        finally
        {
            window.Close();
        }
    }
}
