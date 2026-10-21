using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463: the words and figures of the three synchronization screens, shared by the computer and the phone: the
/// status line, the other devices seen, the side by side of a session changed on two devices, and the two cards' sizes and their basis.
/// </summary>
public sealed class SyncScreensTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-syncscreens-{Guid.NewGuid():N}");
    private readonly DirectorySyncFolder cloud;
    private DateTime clock = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    public SyncScreensTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
    }

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private (SessionStore Store, FileSyncLocal Local) Device(string name)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        return (SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")));
    }

    private static SessionRecord Session(string created) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        "{\"marks\":1}", 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    [Fact]
    public void TheStatusLineSaysWhenAndWhatEachWay()
    {
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        Assert.Null(SyncScreens.Status(new SyncSetup(null, "Desk"), now));
        Assert.Equal("Not synced yet.", SyncScreens.Status(new SyncSetup("C:/Cloud", "Desk"), now));
        var ran = new SyncSetup("C:/Cloud", "Desk", LastUtc: now.AddMinutes(-2).ToString("o"), LastIn: 3, LastOut: 1);
        Assert.Equal("Synced 2 minutes ago: 3 changes in, 1 out.", SyncScreens.Status(ran, now));
        Assert.Equal("Synced just now: nothing new either way.", SyncScreens.Status(ran with { LastUtc = now.ToString("o"), LastIn = 0, LastOut = 0 }, now));
        Assert.Equal("1 session to choose between", SyncScreens.ToChooseBanner(1));
        Assert.Equal("Keep Alan's phone's", SyncScreens.KeepThis("Alan's phone"));
    }

    [Fact]
    public void TheOtherDevicesAreThoseThatWroteToTheFolderNotThisOne()
    {
        var (a, aLocal) = Device("a");
        var (b, bLocal) = Device("b");
        a.Save(Session("2026-10-20T10:00:00Z"));
        SyncEngine.Run(a, aLocal, cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(b, bLocal, cloud, "Phone", clock = clock.AddMinutes(1));
        var seenFromA = SyncScreens.Others(cloud, a.SyncDevice());
        var phone = Assert.Single(seenFromA);
        Assert.Equal("Phone", phone.Name);
        Assert.Equal("Phone, last wrote 3 minutes ago", SyncScreens.SeenLine(phone, clock.AddMinutes(3)));
        Assert.Equal("Desk", Assert.Single(SyncScreens.Others(cloud, b.SyncDevice())).Name);
        Assert.Empty(SyncScreens.Others(new DirectorySyncFolder(Path.Combine(root, "gone")), a.SyncDevice()));
    }

    [Fact]
    public void ASessionChangedOnTwoDevicesIsLaidOutSideBySideWithWhatDiffers()
    {
        var (a, aLocal) = Device("a");
        var (b, bLocal) = Device("b");
        a.Save(Session("2026-10-20T10:00:00Z"));
        SyncEngine.Run(a, aLocal, cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(b, bLocal, cloud, "Phone", clock = clock.AddMinutes(1));
        var onA = a.List().Select(s => a.Get(s.Id)!).Single();
        var onB = b.List().Select(s => b.Get(s.Id)!).Single();
        a.Save(onA with { Load = "H4350, 41.5 gr" });
        b.Save(onB with { MeanRadiusInches = 0.35 });
        SyncEngine.Run(a, aLocal, cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(b, bLocal, cloud, "Phone", clock = clock.AddMinutes(1));
        SyncEngine.Run(a, aLocal, cloud, "Desk", clock = clock.AddMinutes(1));

        var waiting = Assert.Single(a.SyncConflicts());
        var (here, there) = SyncEngine.Versions(a, waiting)!.Value;
        Assert.NotNull(here);
        var side = SyncScreens.Compare(here, there, "Desk", "Phone", waiting.FoundUtc, UnitSettings.Imperial);
        Assert.Contains("GroupLab 5x5 Load Development, Letter", side.Heading, StringComparison.Ordinal);
        Assert.True(side.Rows.Single(r => r.What == "Load").Differs);
        Assert.True(side.Rows.Single(r => r.What == "Mean radius").Differs);
        Assert.False(side.Rows.Single(r => r.What == "Rifle").Differs);
        Assert.Equal("Desk", side.HereDevice);
    }

    [Fact]
    public void TheCardsUseTheOwnPaceWhereThereIsEnoughHistoryAnd200AYearOtherwise()
    {
        var few = SyncScreens.Cards((3, 180_000, 12_000_000, new DateTime(2026, 10, 1), new DateTime(2026, 10, 20)));
        Assert.Contains("assumes 200 sessions", few.Basis, StringComparison.Ordinal);
        Assert.Equal(180_000, few.Marks.NowBytes);
        Assert.Equal(12_180_000, few.Originals.NowBytes);
        Assert.Equal(200 * 60_000, few.Marks.YearBytes);

        var many = SyncScreens.Cards((40, 2_400_000, 160_000_000, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1)));
        Assert.Contains("from your 40 over 181 days", many.Basis, StringComparison.Ordinal);
        Assert.True(many.Originals.YearBytes > many.Marks.YearBytes);
        Assert.Equal("48 KB", SyncScreens.Size(48_000));
        Assert.Equal("3.2 MB", SyncScreens.Size(3_200_000));
        Assert.Equal("1.4 GB", SyncScreens.Size(1_400_000_000));
        Assert.Contains("written again whole", SyncScreens.ScaleLine(1_000_000), StringComparison.Ordinal);
    }
}
