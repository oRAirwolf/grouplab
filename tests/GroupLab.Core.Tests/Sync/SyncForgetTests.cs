using GroupLab.Core.Records;
using GroupLab.Core.Sync;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 471 section 2, question 109 (a): a device that never comes back is forgotten from the Sync block, after which
/// tidying no longer waits for it, nothing in the folder is deleted, and the device is seen again if it syncs again. And a different, empty
/// folder gets every record again, where before the records already written to the last folder never reached it.
/// </summary>
public sealed class SyncForgetTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-forget-{Guid.NewGuid():N}");
    private DateTime clock = DateTime.MinValue;

    public SyncForgetTests() => Directory.CreateDirectory(root);

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private sealed record Device(string Name, SessionStore Store, FileSyncLocal Local, string Database)
    {
        public string Id => Store.SyncDevice();
    }

    private Device NewDevice(string name)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        string database = Path.Combine(folder, "grouplab.db");
        return new Device(name, SessionStore.Open(database), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")), database);
    }

    private DirectorySyncFolder Folder(string name)
    {
        Directory.CreateDirectory(Path.Combine(root, name));
        return new DirectorySyncFolder(Path.Combine(root, name));
    }

    private SyncReport Sync(Device d, double day, ISyncFolder folder)
    {
        var at = Start.AddDays(day);
        clock = at > clock ? at : clock.AddMinutes(1);
        return SyncEngine.Run(d.Store, d.Local, folder, d.Name, clock);
    }

    private static SessionRecord Session(string created, string marking = "{\"marks\":0}") => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        marking, 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    private static void Edit(Device d, string created, string marking)
    {
        var s = d.Store.List().Select(x => d.Store.Get(x.Id)!).Single(x => x.CreatedUtc == created);
        d.Store.Save(s with { MarkingJson = marking });
    }

    private static string Marking(Device d, string created) => d.Store.List().Select(x => d.Store.Get(x.Id)!).Single(x => x.CreatedUtc == created).MarkingJson;

    private string[] Everything(string folder) =>
        [.. Directory.GetFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(root, f)).Order(StringComparer.Ordinal)];

    [Fact]
    public void AForgottenDeviceNoLongerHoldsTidyingBackNothingIsDeletedAndItIsSeenAgainWhenItSyncs()
    {
        var cloud = Folder("cloud");
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string made = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(made));
        Sync(desk, 0, cloud);
        Sync(phone, 0, cloud);

        // The phone goes away; the desk changes the session each day, and tidying waits for the phone.
        for (int day = 1; day <= 3; day++)
        {
            Edit(desk, made, $"{{\"marks\":{day}}}");
            Sync(desk, day, cloud);
        }

        Assert.Equal(0, Sync(desk, 4, cloud).Tidied!.Moved);
        Assert.Contains(SyncScreens.Others(cloud, desk.Id), d => d.Id == phone.Id && d.Name == "phone");

        // Forgetting it deletes nothing in the folder and adds one small file.
        var before = Everything("cloud");
        SyncForget.Forget(cloud, desk.Id, phone.Id, Start.AddDays(4.5));
        var after = Everything("cloud");
        Assert.Empty(before.Except(after));
        Assert.Equal([Path.Combine("cloud", SyncEngine.Root, "forgotten", phone.Id + ".json")], after.Except(before));
        Assert.True(SyncForget.IsForgotten(cloud, phone.Id));
        Assert.DoesNotContain(SyncScreens.Others(cloud, desk.Id), d => d.Id == phone.Id);

        // Now the desk tidies every file but its newest (the first, which the phone read, went on day 1), and the phone's own files stay.
        var tidied = Sync(desk, 5, cloud);
        Assert.Equal(2, tidied.Tidied!.Moved);
        Assert.Equal(before.Count(f => f.Contains(Path.Combine("changes", phone.Id), StringComparison.Ordinal)),
            Everything("cloud").Count(f => f.Contains(Path.Combine("changes", phone.Id), StringComparison.Ordinal)));

        // The phone comes back: it ends with the newest version, and is listed again.
        Sync(phone, 6, cloud);
        Assert.Equal("{\"marks\":3}", Marking(phone, made));
        Assert.Empty(phone.Store.SyncConflicts());
        Assert.False(SyncForget.IsForgotten(cloud, phone.Id));
        Assert.Contains(SyncScreens.Others(cloud, desk.Id), d => d.Id == phone.Id);
    }

    [Fact]
    public void ADeviceDoesNotForgetItselfOrAPathOutsideTheFolder()
    {
        var cloud = Folder("cloud");
        Assert.Throws<ArgumentException>(() => SyncForget.Forget(cloud, "abc", "abc", Start));
        Assert.Throws<ArgumentException>(() => SyncForget.Forget(cloud, "abc", "../x", Start));
        Assert.Throws<ArgumentException>(() => SyncForget.Forget(cloud, "abc", "", Start));
    }

    [Fact]
    public void ADifferentEmptyFolderGetsEveryRecordAgain()
    {
        var first = Folder("first");
        var desk = NewDevice("desk");
        desk.Store.Save(Session("2026-10-20T10:00:00Z"));
        desk.Store.Save(Session("2026-10-21T10:00:00Z"));
        Assert.Equal(2, Sync(desk, 0, first).Written);
        Assert.Equal(0, Sync(desk, 0, first).Written);

        // A different, empty folder: everything this device holds is written there too, and a device joining there gets it.
        var second = Folder("second");
        Assert.Equal(2, Sync(desk, 1, second).Written);
        var tablet = NewDevice("tablet");
        Sync(tablet, 1, second);
        Assert.Equal(2, tablet.Store.List().Count);
        Assert.Equal(0, Sync(desk, 1, second).Written);
    }

    [Fact]
    public void ADeviceThatSyncedBeforeTheFolderHadAnIdentityDoesNotWriteEverythingAgain()
    {
        var cloud = Folder("cloud");
        var desk = NewDevice("desk");
        desk.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(desk, 0, cloud);

        // As a device that last synced with an older GroupLab: no identity in the folder, none known here, its change files there.
        File.Delete(Path.Combine(root, "cloud", SyncEngine.Root, "folder.json"));
        using (var db = new SqliteConnection($"Data Source={desk.Database};Pooling=False"))
        {
            db.Open();
            using var command = db.CreateCommand();
            command.CommandText = "DELETE FROM meta WHERE key = 'sync_folder'";
            command.ExecuteNonQuery();
        }

        Assert.Null(desk.Store.SyncFolderId());
        Assert.Equal(0, Sync(desk, 1, cloud).Written);
        Assert.NotNull(desk.Store.SyncFolderId());
    }
}
