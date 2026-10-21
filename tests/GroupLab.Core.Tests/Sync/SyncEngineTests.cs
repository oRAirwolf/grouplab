using System.Text.Json.Nodes;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1 item 6: synchronization through one folder, as option A builds it, with simulated devices that
/// each have their own database, sheets and settings and share one folder. Nothing is ever lost: before a merge changes or takes away a
/// record, the record as it was is kept.
/// </summary>
public sealed class SyncEngineTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-sync-{Guid.NewGuid():N}");
    private readonly DirectorySyncFolder cloud;
    private DateTime clock = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    public SyncEngineTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
    }

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private sealed record Device(string Name, SessionStore Store, FileSyncLocal Local);

    private Device NewDevice(string name)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        return new Device(name, SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")));
    }

    private SyncReport Sync(Device d, ISyncFolder? folder = null, SyncOptions? options = null)
    {
        clock = clock.AddMinutes(1);
        return SyncEngine.Run(d.Store, d.Local, folder ?? cloud, d.Name, clock, options);
    }

    private static SessionRecord Session(string created, string marking = "{\"marks\":1}", string sha = "aa") => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        marking, 25, 0.3, 0.25, 0.4, null, new string(sha[0], 64), [1, 2, 3], "image/jpeg");

    private static SessionRecord? Only(SessionStore store) => store.List().Select(s => store.Get(s.Id)).SingleOrDefault();

    [Fact]
    public void TwoDevicesShareASessionARifleASheetAndASetting()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        long id = desk.Store.Save(Session("2026-10-20T10:00:00Z"));
        desk.Store.AddChronographString(id, "Xero", "2026-10-20T10:01:00Z", [2710, 2715, 2702]);
        desk.Store.SaveBook(RecordBook.Empty.With(new Rifle("Tikka", 0.25, AngularUnit.Moa)));
        desk.Local.SaveSheet(new ExportedSheet("mine.gltd.json", "{\"name\":\"mine\"}"));
        desk.Local.SaveSetting("scope", "Mil");

        var wrote = Sync(desk);
        Assert.True(wrote.Reached);
        Assert.Equal(4, wrote.Written);
        var read = Sync(phone);
        Assert.Equal(4, read.Applied);

        var got = Only(phone.Store)!;
        Assert.Equal("{\"marks\":1}", got.MarkingJson);
        Assert.Equal([1, 2, 3], got.ProofImage);
        Assert.Equal([2710, 2715, 2702], phone.Store.ChronographStrings(got.Id).Single().VelocitiesFps);
        Assert.Equal("Tikka", phone.Store.LoadBook().Rifles.Single().Name);
        Assert.Equal("mine.gltd.json", phone.Local.Sheets().Single().File);
        Assert.Equal("Mil", (string?)phone.Local.Settings()["scope"]);

        // Nothing new on either side: nothing written, nothing read again, nothing taken for a change.
        Assert.Equal(0, Sync(phone).Written);
        Assert.Equal(0, Sync(desk).Written);
        Assert.Equal(0, Sync(phone).Applied);
    }

    [Fact]
    public void ThreeDevicesFollowTheLatestChangeWhereverItWasMade()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        var c = NewDevice("c");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(a);
        Sync(b);
        var onB = Only(b.Store)!;
        b.Store.Save(onB with { MarkingJson = "{\"marks\":2}", ShotCount = 24 });
        Sync(b);
        Sync(c);
        Sync(a);
        Assert.Equal("{\"marks\":2}", Only(c.Store)!.MarkingJson);
        Assert.Equal("{\"marks\":2}", Only(a.Store)!.MarkingJson);
        Assert.Empty(a.Store.SyncConflicts());

        // A's own version before B's change came in is kept, so it can be had back.
        var kept = a.Store.Kept().Single();
        Assert.Contains("\\u0022marks\\u0022:1", kept.Payload.Replace("\\\"", "\\u0022", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("newer version from device", kept.Why, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSameSessionChangedOnTwoDevicesIsKeptBothWaysAndListed()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(a);
        Sync(b);
        a.Store.Save(Only(a.Store)! with { MarkingJson = "{\"marks\":\"a\"}" });
        b.Store.Save(Only(b.Store)! with { MarkingJson = "{\"marks\":\"b\"}" });
        Sync(a);
        Sync(b);
        Sync(a);

        // Each keeps its own and lists the other's; nothing was decided.
        Assert.Equal("{\"marks\":\"a\"}", Only(a.Store)!.MarkingJson);
        Assert.Equal("{\"marks\":\"b\"}", Only(b.Store)!.MarkingJson);
        var onA = Assert.Single(a.Store.SyncConflicts());
        Assert.Contains("marks\\u0022:\\u0022b", onA.Payload!.Replace("\\\"", "\\u0022", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Single(b.Store.SyncConflicts());

        // The person chooses B's on A; it travels, and B's list is settled by it.
        SyncEngine.Resolve(a.Store, a.Local, onA.Id, SyncChoice.KeepTheOther, clock);
        Assert.Equal("{\"marks\":\"b\"}", Only(a.Store)!.MarkingJson);
        Assert.Contains(a.Store.Kept(), k => k.Why.Contains("replaced as chosen", StringComparison.Ordinal));
        Sync(a);
        Sync(b);
        Assert.Empty(a.Store.SyncConflicts());
        Assert.Empty(b.Store.SyncConflicts());
        Assert.Equal("{\"marks\":\"b\"}", Only(b.Store)!.MarkingJson);
    }

    [Fact]
    public void KeepingBothMakesTheOtherDevicesVersionASessionOfItsOwn()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(a);
        Sync(b);
        a.Store.Save(Only(a.Store)! with { Load = "Varget" });
        b.Store.Save(Only(b.Store)! with { Load = "N150" });
        Sync(a);
        Sync(b);
        var waiting = Assert.Single(b.Store.SyncConflicts());
        SyncEngine.Resolve(b.Store, b.Local, waiting.Id, SyncChoice.KeepBoth, clock);
        Assert.Equal(["N150", "Varget"], b.Store.List().Select(s => s.Load).Order());
        Sync(b);
        Sync(a);
        Assert.Equal(["N150", "Varget"], a.Store.List().Select(s => s.Load).Order());
    }

    [Fact]
    public void ADeletionAgainstAnEditKeepsTheEditAndSaysSo()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(a);
        Sync(b);
        a.Store.Delete(Only(a.Store)!.Id);
        b.Store.Save(Only(b.Store)! with { MarkingJson = "{\"marks\":\"edited\"}" });
        Sync(a);
        Sync(b);
        Sync(a);
        Assert.Equal("{\"marks\":\"edited\"}", Only(a.Store)!.MarkingJson);
        Assert.Equal("{\"marks\":\"edited\"}", Only(b.Store)!.MarkingJson);
        Assert.Contains(a.Store.SyncConflicts(), c => c.Payload is null);
        Assert.Contains(b.Store.SyncConflicts(), c => c.Payload is null);

        // A deletion with no edit against it travels, and the session taken away is kept.
        b.Store.Delete(Only(b.Store)!.Id);
        Sync(b);
        Sync(a);
        Assert.Empty(a.Store.List());
        Assert.Contains(a.Store.Kept(), k => k.Why.Contains("taken away on device", StringComparison.Ordinal));
    }

    [Fact]
    public void AHalfWrittenChangeFileIsReadOnceItHasArrivedWhole()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(a);
        string device = Directory.GetDirectories(Path.Combine(root, "cloud", SyncEngine.Root, "changes")).Single();
        string file = Directory.GetFiles(device).Single();
        byte[] whole = File.ReadAllBytes(file);
        File.WriteAllBytes(file, whole[..(whole.Length / 2)]);

        var first = Sync(b);
        Assert.Equal(1, first.FilesWaiting);
        Assert.Empty(b.Store.List());

        File.WriteAllBytes(file, whole);
        var second = Sync(b);
        Assert.Equal(0, second.FilesWaiting);
        Assert.Single(b.Store.List());
    }

    [Fact]
    public void ADeviceBackAfterMonthsCatchesUpAndADeletionStillReachesIt()
    {
        var a = NewDevice("a");
        var away = NewDevice("away");
        a.Store.Save(Session("2026-01-01T10:00:00Z", sha: "aa"));
        a.Store.Save(Session("2026-01-02T10:00:00Z", sha: "bb"));
        Sync(a);
        Sync(away);
        Assert.Equal(2, away.Store.List().Count);

        // Months of work while it is away: one session taken away, three added; and the 90 days' markers pruned here meanwhile.
        a.Store.Delete(a.Store.List().Single(s => s.CreatedUtc.StartsWith("2026-01-01", StringComparison.Ordinal)).Id);
        Sync(a);
        for (int i = 0; i < 3; i++)
        {
            a.Store.Save(Session($"2026-0{i + 3}-01T10:00:00Z", sha: "c" + i));
            Sync(a);
        }

        clock = clock.AddDays(200);
        Sync(a);
        Assert.DoesNotContain(a.Store.SyncLedger().Values, e => e.DeletedUtc is not null);

        Sync(away);
        Assert.Equal(a.Store.List().Select(s => s.CreatedUtc).Order(), away.Store.List().Select(s => s.CreatedUtc).Order());
        Assert.Equal(4, away.Store.List().Count);
        Assert.Contains(away.Store.Kept(), k => k.Why.Contains("taken away on device", StringComparison.Ordinal));

        // And nothing the device kept comes back to A.
        Sync(away);
        Sync(a);
        Assert.Equal(4, a.Store.List().Count);
    }

    [Fact]
    public void AFolderThatDisappearsChangesNothingAndSaysSo()
    {
        var a = NewDevice("a");
        a.Store.Save(Session("2026-10-20T10:00:00Z"));
        var gone = new DirectorySyncFolder(Path.Combine(root, "unplugged"));
        var report = Sync(a, gone);
        Assert.False(report.Reached);
        Assert.Contains("not there", report.Said.Single(), StringComparison.Ordinal);
        Assert.Empty(a.Store.SyncLedger());
        Assert.Single(a.Store.List());

        // Back again, everything is written as if nothing had happened.
        Assert.Equal(1, Sync(a).Written);
    }

    [Fact]
    public void NothingIsLostEveryRecordAMergeReplacedIsKeptWhole()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.SaveBook(RecordBook.Empty.With(new Load("H4350 41.5", "140 ELD-M") { MuzzleVelocityFps = 2710 }));
        a.Local.SaveSheet(new ExportedSheet("mine.gltd.json", "{\"v\":1}"));
        Sync(a);
        Sync(b);
        b.Store.SaveBook(RecordBook.Empty.With(new Load("H4350 41.5", "140 ELD-M") { MuzzleVelocityFps = 2725 }));
        b.Local.SaveSheet(new ExportedSheet("mine.gltd.json", "{\"v\":2}"));
        Sync(b);
        Sync(a);
        Assert.Equal(2725, a.Store.LoadBook().Loads.Single().MuzzleVelocityFps);
        Assert.Equal("{\"v\":2}", a.Local.Sheets().Single().Definition);

        var kept = a.Store.Kept();
        Assert.Equal(2, kept.Count);
        Assert.Contains(kept, k => k.Kind == SyncEngine.LoadKind && (double?)JsonNode.Parse(k.Payload)!["muzzleVelocityFps"] == 2710);
        Assert.Contains(kept, k => k.Kind == SyncEngine.SheetKind && ((string?)JsonNode.Parse(k.Payload)!["definition"])!.Contains("\"v\":1", StringComparison.Ordinal));
    }

    [Fact]
    public void TheOriginalPhotographsTravelOnlyWhenAskedFor()
    {
        var a = NewDevice("a");
        string photo = Path.Combine(root, "photo.jpg");
        File.WriteAllBytes(photo, [0xFF, 0xD8, 0xFF, 0xD9]);
        a.Store.Save(Session("2026-10-20T10:00:00Z") with { ImagePath = photo });
        Assert.Equal(0, Sync(a).OriginalsCopied);
        Assert.False(Directory.Exists(Path.Combine(root, "cloud", SyncEngine.Root, "originals")));
        Assert.Equal(1, Sync(a, options: new SyncOptions(Originals: true)).OriginalsCopied);
        Assert.Equal(0, Sync(a, options: new SyncOptions(Originals: true)).OriginalsCopied);

        // The original photograph's path on this device never travels.
        string changes = string.Concat(Directory.GetFiles(Path.Combine(root, "cloud", SyncEngine.Root, "changes"), "*.json", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.DoesNotContain("photo.jpg", changes, StringComparison.Ordinal);
    }

    [Fact]
    public void EachDeviceWritesOnlyItsOwnFilesAndNoneIsLeftHalfWritten()
    {
        var a = NewDevice("a");
        var b = NewDevice("b");
        a.Store.Save(Session("2026-10-20T10:00:00Z", sha: "aa"));
        b.Store.Save(Session("2026-10-20T11:00:00Z", sha: "bb"));
        Sync(a);
        Sync(b);
        Sync(a);
        var folders = Directory.GetDirectories(Path.Combine(root, "cloud", SyncEngine.Root, "changes")).Select(f => Path.GetFileName(f)!).ToList();
        Assert.Equal(new[] { a.Store.SyncDevice(), b.Store.SyncDevice() }.Order(StringComparer.Ordinal), folders.Order(StringComparer.Ordinal));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "cloud"), ".*", SearchOption.AllDirectories));
        Assert.Equal(2, a.Store.List().Count);
        Assert.Equal(2, b.Store.List().Count);
    }

    [Fact]
    public void RevisionsTellAChangeFromTheVersionItReplaced()
    {
        var one = VersionVector.Empty.Bump("a");
        var two = one.Bump("a");
        var other = one.Bump("b");
        Assert.Equal(Order.Newer, two.Compare(one));
        Assert.Equal(Order.Older, one.Compare(two));
        Assert.Equal(Order.Concurrent, two.Compare(other));
        Assert.Equal(Order.Newer, two.Merge(other).Compare(other));
        Assert.Equal(Order.Same, VersionVector.Parse(two.ToString()).Compare(two));
    }
}
