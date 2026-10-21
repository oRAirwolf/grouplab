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
/// NOTES-FROM-PLANNING.md entry 463 on the phone: the Sync page under Settings chooses a folder through the platform, runs, says when and what,
/// shows the cards stacked and stops without deleting anything; a folder gone is said on the page; and a session changed on two devices is
/// found from a banner on Sessions and chosen between on a centred sheet. Every one laid out whole at a phone's width.
/// </summary>
public sealed class Entry463PhoneTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-463p-{Guid.NewGuid():N}");

    public Entry463PhoneTests()
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

    private static SessionRecord Session(string created) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        "{\"marks\":1}", 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    [AvaloniaFact]
    public void TheSyncPageChoosesAFolderRunsAndStopsWithoutDeletingAnything()
    {
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            shell.ShowSync();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(SyncScreens.Explained, Words(window));
            TestPhone.SyncFolderToPick = Path.Combine(root, "cloud");
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());
            window.UpdateLayout();
            var said = Words(window).ToList();
            Assert.Contains(said, w => w.StartsWith("Synced just now", StringComparison.Ordinal));
            Assert.Contains(SyncScreens.NoOtherDevices, said);
            Assert.Contains("Marks and proof pictures", said);
            Assert.Contains(SyncScreens.StopsHereOnly, said);
            var laid = LayoutAudit.Findings(window);
            Assert.True(laid.Count == 0, string.Join("; ", laid));

            PhoneSync.Stop();
            Dispatcher.UIThread.RunJobs();
            Assert.False(PhoneSync.Setup.On);
            Assert.True(Directory.EnumerateFiles(Path.Combine(root, "cloud", SyncEngine.Root), "*", SearchOption.AllDirectories).Any());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AFolderGoneIsSaidOnThePage()
    {
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            TestPhone.SyncFolderToPick = Path.Combine(root, "unplugged");
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());
            shell.ShowSync();
            Dispatcher.UIThread.RunJobs();
            Assert.True(PhoneSync.Unreachable);
            Assert.Contains(SyncScreens.UnreachableTitle, Words(window));
            Assert.Contains(SyncScreens.NothingLost, Words(window));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ASessionChangedOnTwoDevicesIsChosenBetweenFromSessions()
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
        var cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
        TestPhone.SyncFolderToPick = Path.Combine(root, "cloud");
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            mine.Save(Session("2026-10-20T10:00:00Z"));
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());
            SyncEngine.Run(other, otherLocal, cloud, "Desk", DateTime.UtcNow.AddMinutes(1));
            mine.Save(mine.Get(mine.List().Single().Id)! with { Load = "H4350, 41.5 gr" });
            other.Save(other.Get(other.List().Single().Id)! with { MeanRadiusInches = 0.35 });
            Wait(PhoneSync.Now());
            SyncEngine.Run(other, otherLocal, cloud, "Desk", DateTime.UtcNow.AddMinutes(2));
            Wait(PhoneSync.Now());

            Assert.Single(PhoneSync.ToChoose());
            shell.Show(Shell.Place.Sessions);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(SyncScreens.ToChooseBanner(1), Words(window));
            Assert.Contains(Words(window), w => w.StartsWith(SyncScreens.TwoVersions, StringComparison.Ordinal));

            var page = Assert.IsType<SessionsPage>(shell.PageContent);
            PhoneSync.ShowConflict(page, PhoneSync.ToChoose()[0].Conflict.Id, () => { });
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.Contains(SyncScreens.ConflictTitle, Words(window));
            Assert.Contains(SyncScreens.Differs, Words(window));
            var choose = window.GetLogicalDescendants().OfType<Button>().First(b => Equals(b.Content, SyncScreens.KeepBoth) || (b.Content is TextBlock t && t.Text == SyncScreens.KeepBoth));
            choose.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(PhoneSync.ToChoose());
            Assert.Equal(2, mine.List().Count);
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
