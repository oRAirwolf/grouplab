using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463, on the computer: Settings' "Sync between devices" (1A) chooses a folder, runs, says when and what, lists
/// the devices seen, shows the two cards (3B) and stops without deleting anything; a folder that cannot be reached is said in the section and,
/// once a run, in the centred warning; and a session changed on two devices is found from Sessions and laid out side by side (2A).
/// </summary>
public sealed class Entry463Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-463-{Guid.NewGuid():N}");

    public Entry463Tests() => Directory.CreateDirectory(Path.Combine(root, "cloud"));

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private string Cloud => Path.Combine(root, "cloud");

    private MainWindow NewWindow()
    {
        var store = new AppSettingsStore(Path.Combine(root, $"desk-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
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

    private static SessionRecord Session(string created) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        "{\"marks\":1}", 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    [AvaloniaFact]
    public void ChoosingAFolderSyncsSaysWhatItDidAndStoppingDeletesNothing()
    {
        var window = NewWindow();
        try
        {
            Assert.False(window.Sync.On);
            Assert.Contains(SyncScreens.Explained, window.SyncText);
            window.Sessions!.Save(Session("2026-10-20T10:00:00Z"));
            window.ChooseSyncFolder(Cloud);
            Wait(window.SyncNowAsync());
            var said = window.SyncText.ToList();
            Assert.Contains(Cloud, said);
            Assert.Contains(said, t => t.StartsWith("Synced just now: 0 changes in, ", StringComparison.Ordinal));
            Assert.Contains(SyncScreens.NoOtherDevices, said);
            Assert.Contains("Marks and proof pictures", said);
            Assert.Contains("Also the original photographs", said);
            Assert.Contains(SyncScreens.StopsHereOnly, said);
            Assert.True(Directory.Exists(Path.Combine(Cloud, SyncEngine.Root, "changes")));

            window.StopSyncing();
            Assert.False(window.Sync.On);
            Assert.True(Directory.EnumerateFiles(Path.Combine(Cloud, SyncEngine.Root), "*", SearchOption.AllDirectories).Any());
            Assert.Single(window.Sessions.List());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AFolderThatCannotBeReachedIsSaidInTheSectionAndOnceInTheMiddle()
    {
        var window = NewWindow();
        try
        {
            window.ChooseSyncFolder(Path.Combine(root, "unplugged"));
            Wait(window.SyncNowAsync());
            Assert.Equal(SyncScreens.UnreachableTitle, window.ProblemTitle);
            Assert.Contains(SyncScreens.UnreachableTitle, window.SyncText);
            Assert.Contains(SyncScreens.NothingLost, window.SyncText);
            window.DismissProblem();
            Wait(window.SyncNowAsync());
            Assert.NotEqual(SyncScreens.UnreachableTitle, window.ProblemTitle);
            Assert.True(window.Sync.On);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ASessionChangedOnTwoDevicesIsFoundOnSessionsAndKeptBothWays()
    {
        var window = NewWindow();
        string phone = Path.Combine(root, "phone");
        Directory.CreateDirectory(phone);
        var other = SessionStore.Open(Path.Combine(phone, "grouplab.db"));
        var otherLocal = new FileSyncLocal(Path.Combine(phone, "sheets"), Path.Combine(phone, "settings.json"));
        var cloud = new DirectorySyncFolder(Cloud);
        try
        {
            var mine = window.Sessions!;
            mine.Save(Session("2026-10-20T10:00:00Z"));
            window.ChooseSyncFolder(Cloud);
            Wait(window.SyncNowAsync());
            SyncEngine.Run(other, otherLocal, cloud, "Alan's phone", DateTime.UtcNow.AddMinutes(1));
            mine.Save(mine.Get(mine.List().Single().Id)! with { Load = "H4350, 41.5 gr" });
            other.Save(other.Get(other.List().Single().Id)! with { MeanRadiusInches = 0.35 });
            Wait(window.SyncNowAsync());
            SyncEngine.Run(other, otherLocal, cloud, "Alan's phone", DateTime.UtcNow.AddMinutes(2));
            Wait(window.SyncNowAsync());

            var waiting = Assert.Single(window.SessionsToChoose());
            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(SyncScreens.ToChooseBanner(1), window.SessionsText);
            Assert.Contains(SyncScreens.TwoVersions, window.SessionsText);

            window.ShowSyncConflict(waiting.Conflict.Id);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(SyncScreens.ConflictTitle, window.ProblemTitle);
            window.PressProblemChoice(SyncScreens.KeepBoth);
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(window.SessionsToChoose());
            Assert.Equal(2, mine.List().Count);
        }
        finally
        {
            window.Close();
        }
    }
}
