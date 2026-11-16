using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495, question 113 answered (b): a pooled set saved as a record of its own. It names its sheets, never copies
/// their shots, so it reopens with the same figures and follows a hole fixed on a sheet; deleting it keeps its sheets; deleting a sheet says
/// which pools use it; and it travels whole in a shared file, an export, an import and synchronization.
/// </summary>
public sealed class PoolRecordTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-pools-{Guid.NewGuid():N}");

    public PoolRecordTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GroupLab.Tests.Support.Temp.Delete(root);
    }

    private SessionStore Store(string name)
    {
        Directory.CreateDirectory(Path.Combine(root, name));
        return SessionStore.Open(Path.Combine(root, name, "grouplab.db"));
    }

    /// <summary>A picture with a scale and three shots about <paramref name="x"/>, as Accept and analyze would save it.</summary>
    private static MarkingState Picture(double x)
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(600, 0), 1));
        session.AddShot(new PointD(x, x));
        session.AddShot(new PointD(x + 30, x + 10));
        session.AddShot(new PointD(x + 5, x + 40));
        return MarkingFile.Read(MarkingFile.Write(session.State)).State;
    }

    private static SessionRecord Record(MarkingState state, string hash, string created) =>
        SessionRecords.Build(state, null, UnitSettings.Imperial, false, null, hash, null, null, DateTime.Parse(created, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal), DateTime.Parse(created, System.Globalization.CultureInfo.InvariantCulture));

    private static List<long> ThreeSheets(SessionStore store) =>
    [
        store.Save(Record(Picture(100), "aa", "2026-11-10T10:00:00Z")),
        store.Save(Record(Picture(200), "bb", "2026-11-11T10:00:00Z")),
        store.Save(Record(Picture(300), "cc", "2026-11-12T10:00:00Z")),
    ];

    private static IReadOnlyList<PointD> Shots(MarkingState state) => [.. state.Shots.Select(s => s.Image)];

    [Fact]
    public void APoolSavedAndReopenedGivesTheSameFiguresAndFollowsAHoleFixedOnASheet()
    {
        var store = Store("a");
        var ids = ThreeSheets(store);
        var pool = SessionPools.Save(store, null, ids, SessionPools.Choices(pictures: true), DateTime.UtcNow);
        Assert.True(pool.Id > 0);
        Assert.True(pool.Pictures);
        Assert.Equal(ids.Select(id => DataExport.Identity(store.Get(id)!)), pool.Sheets);
        Assert.Equal(3, store.List().Count);

        // Saved again with the same sheets, the same pool, not a second one.
        Assert.Equal(pool.Id, SessionPools.Save(store, null, ids, SessionPools.Choices(pictures: true), DateTime.UtcNow).Id);
        Assert.Single(store.Pools());

        // Reopened, the pool is worked out from its sheets: the same shots and figures as pooling them afresh.
        var fresh = SetPool.CompositeOfPictures([.. ids.Select(id => MarkingFile.Read(store.Get(id)!.MarkingJson).State)]);
        var opened = SessionPools.Open(store, store.GetPool(pool.Id)!);
        Assert.Equal(Shots(fresh), Shots(opened.Composite));
        Assert.Equal(GroupAnalysis.Analyse(fresh).Counted?.MeanRadius?.Value, GroupAnalysis.Analyse(opened.Composite).Counted?.MeanRadius?.Value);
        Assert.Equal(ids, opened.SessionIds);
        Assert.False(opened.TooFew);

        // A hole moved on the second sheet moves in the pool too.
        var second = store.Get(ids[1])!;
        var state = MarkingFile.Read(second.MarkingJson).State;
        var session = new MarkingSession(state);
        session.MoveShot(state.Shots[0].Id, new PointD(260, 260));
        store.Save(SessionRecords.Build(session.State, null, UnitSettings.Imperial, false, second, "bb", null, null, DateTime.UtcNow, DateTime.Now));
        var after = SessionPools.Open(store, store.GetPool(pool.Id)!);
        Assert.NotEqual(Shots(opened.Composite), Shots(after.Composite));
        Assert.Equal(3, after.Sheets.Count);

        // Its row in Session records: a pooled set of 3 sheets, dated by its newest sheet, with the pooled shots.
        var row = Assert.Single(SessionPools.Rows(store, UnitSettings.Imperial)).Row;
        Assert.Equal(SessionPools.RowId(pool.Id), row.Id);
        Assert.Equal(pool.Id, SessionPools.PoolOf(row.Id));
        Assert.StartsWith("Pooled set of 3 sheets", row.SheetName, StringComparison.Ordinal);
        Assert.Equal("2026-11-12", row.ShotDate);
        Assert.Equal(9, row.ShotCount);
        var merged = SessionPools.Merged(store.List(), [row]);
        Assert.Equal(4, merged.Count);
        Assert.Equal(row.Id, merged[0].Id);

        // Compare takes it as one group beside the sessions.
        var chosen = SessionPools.Chosen(store, UnitSettings.Imperial, [row.Id, ids[0]]);
        Assert.Equal([ids[0], row.Id], chosen.Select(c => c.Id));
        var setup = CompareSessions.From(chosen, UnitSettings.Imperial);
        Assert.Equal(2, setup.Groups.Count);
        Assert.Equal(9, setup.Groups[1].Offsets.Count);
    }

    [Fact]
    public void DeletingAPoolKeepsItsSheetsAndDeletingASheetNamesThePoolsThatUseIt()
    {
        var store = Store("a");
        var ids = ThreeSheets(store);
        var pool = SessionPools.Save(store, null, ids, SessionPools.Choices(pictures: true), DateTime.UtcNow);
        var pair = SessionPools.Save(store, null, ids.Take(2).ToList(), SessionPools.Choices(pictures: true), DateTime.UtcNow);

        // The first sheet is in both pools, and the question says so and what happens to them.
        var using_ = store.PoolsUsing(store.Get(ids[0])!);
        Assert.Equal([pool.Id, pair.Id], using_.Select(p => p.Id).Order());
        string question = SessionPools.DeleteSheetQuestion([.. using_.Select(p => (p, SessionPools.RowName(p.Sheets.Count)))])!;
        Assert.Contains("2 pooled sets", question, StringComparison.Ordinal);
        Assert.Contains("a pool left with one sheet says so when it is opened", question, StringComparison.Ordinal);
        Assert.Null(SessionPools.DeleteSheetQuestion([]));

        // Deleted, the sheet leaves both pools, which keep their other sheets; the pair left with one says so when opened.
        Assert.True(store.Delete(ids[0]));
        Assert.Equal(2, store.GetPool(pool.Id)!.Sheets.Count);
        var lone = SessionPools.Open(store, store.GetPool(pair.Id)!);
        Assert.True(lone.TooFew);
        Assert.Contains(SessionPools.TooFewWords(1), lone.Said, StringComparison.Ordinal);

        // Deleting a pool deletes only the pool.
        Assert.True(store.DeletePool(pool.Id));
        Assert.Null(store.GetPool(pool.Id));
        Assert.Equal(2, store.List().Count);
        Assert.Equal("Deleted the pooled set. Its 2 sheets are still in Session records.", SessionPools.DeletedWords(2));
    }

    [Fact]
    public void ASharedPoolOpensWholeOnASecondGroupLabAndOpeningItAgainMakesNothingTwice()
    {
        var store = Store("a");
        var ids = ThreeSheets(store);
        var pool = SessionPools.Save(store, null, ids, SessionPools.Choices(pictures: true), DateTime.UtcNow);
        var files = ids.Select((id, i) =>
        {
            using var one = new MemoryStream();
            SessionPackage.Write(one, MarkingFile.Read(store.Get(id)!.MarkingJson).State, null, UnitSettings.Imperial, [1, 2, (byte)i], ".png", "test", 1, DateTime.UtcNow);
            return one.ToArray();
        }).ToList();
        using var file = new MemoryStream();
        PoolPackage.Write(file, pool.Choices!, files, "GroupLab on the computer", DateTime.UtcNow);
        file.Position = 0;
        Assert.True(PoolPackage.IsPool(file));
        Assert.Equal(0, file.Position);

        // A session file is not a pool's, and a GroupLab from before pools says this one is from a newer GroupLab.
        using (var single = new MemoryStream(files[0]))
        {
            Assert.False(PoolPackage.IsPool(single));
        }

        var older = Assert.Throws<SessionPackageException>(() => SessionPackage.Read(new MemoryStream(file.ToArray())));
        Assert.Contains("newer GroupLab", older.Message, StringComparison.Ordinal);

        var elsewhere = Store("b");
        var contents = PoolPackage.Read(new MemoryStream(file.ToArray()));
        SessionRecord Build(MarkingState state, GroupLab.Core.Gltd.Model.TargetDefinition? definition, string hash) =>
            SessionRecords.Build(state, definition, UnitSettings.Imperial, false, null, hash, null, null, DateTime.UtcNow, DateTime.Now);
        var brought = PoolPackage.BringIn(elsewhere, contents, Path.Combine(root, "b", "shared"), Build, DateTime.UtcNow);
        Assert.Equal(3, brought.Added);
        Assert.Equal(3, elsewhere.List().Count);
        var there = SessionPools.Open(elsewhere, Assert.Single(elsewhere.Pools()));
        Assert.Equal(3, there.Sheets.Count);
        Assert.Equal(9, there.Composite.Shots.Count);
        Assert.True(there.Pool.Pictures);
        Assert.Contains("the pool is saved with them", PoolPackage.OpenedWords(brought, contents.Device), StringComparison.Ordinal);

        var again = PoolPackage.BringIn(elsewhere, PoolPackage.Read(new MemoryStream(file.ToArray())), Path.Combine(root, "b", "shared2"), Build, DateTime.UtcNow);
        Assert.Equal((0, 3), (again.Added, again.AlreadyHere));
        Assert.Equal(3, elsewhere.List().Count);
        Assert.Single(elsewhere.Pools());

        // A damaged file is refused in words.
        var damaged = file.ToArray()[..(int)(file.Length / 2)];
        Assert.Throws<SessionPackageException>(() => PoolPackage.Read(new MemoryStream(damaged)));
    }

    [Fact]
    public void APoolSurvivesExportImportAndSynchronization()
    {
        var store = Store("a");
        var ids = ThreeSheets(store);
        var pool = SessionPools.Save(store, null, ids, SessionPools.Choices(pictures: true), DateTime.UtcNow);

        // Export all my data, then Import data on an empty GroupLab: the pool comes with its sheets; imported again, nothing new.
        using var exported = new MemoryStream();
        DataExport.Write(exported, store, [], [], "test", DateTime.UtcNow);
        var read = DataExport.Read(new MemoryStream(exported.ToArray()));
        Assert.Equal(pool.Key, Assert.Single(read.Pools).Key);
        var other = Store("b");
        var plan = DataExport.Plan(read, other, [], []);
        Assert.Single(plan.NewPools);
        Assert.Contains("1 new pooled set, 0 already here", plan.Summary());
        DataExport.Apply(plan, other);
        Assert.Equal(9, SessionPools.Open(other, Assert.Single(other.Pools())).Composite.Shots.Count);
        var twice = DataExport.Plan(read, other, [], []);
        Assert.Empty(twice.NewPools);
        Assert.Equal(1, twice.SamePools);
        Assert.Empty(twice.Conflicts);

        // A file from before pools reads, with none.
        var before = JsonNode.Parse(exported.ToArray())!.AsObject();
        before.Remove("pools");
        Assert.Empty(DataExport.Read(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(before.ToJsonString()))).Pools);

        // The database's own JSON export round trips its pools exactly.
        var copy = Store("c");
        copy.Import(store.Export());
        Assert.Equal(store.Export(), copy.Export());
        Assert.True(Assert.Single(copy.Pools()).SameAs(pool with { Id = copy.Pools()[0].Id }));

        // Synchronization: a pool made on one device arrives on the other, an update follows it, and a deletion takes it away.
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        var cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
        var clock = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
        FileSyncLocal Local(string name) => new(Path.Combine(root, name, "sheets"), Path.Combine(root, name, "settings.json"));
        var phone = Store("phone");
        SyncEngine.Run(store, Local("a"), cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(phone, Local("phone"), cloud, "Phone", clock = clock.AddMinutes(1));
        var arrived = Assert.Single(phone.Pools());
        Assert.Equal(pool.Key, arrived.Key);
        Assert.Equal(3, SessionPools.Open(phone, arrived).Sheets.Count);

        SessionPools.Save(store, pool.Id, ids.Take(2).ToList(), pool.Choices!, clock);
        SyncEngine.Run(store, Local("a"), cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(phone, Local("phone"), cloud, "Phone", clock = clock.AddMinutes(1));
        Assert.Equal(2, Assert.Single(phone.Pools()).Sheets.Count);

        store.DeletePool(pool.Id);
        SyncEngine.Run(store, Local("a"), cloud, "Desk", clock = clock.AddMinutes(1));
        SyncEngine.Run(phone, Local("phone"), cloud, "Phone", clock = clock.AddMinutes(1));
        Assert.Empty(phone.Pools());
        Assert.Equal(3, phone.List().Count);
    }

    [Fact]
    public void TwoSheetsSharingAnIdentityAreBothInThePoolUntilOneIsDeleted()
    {
        // Two sheets with no picture saved in the same second share an identity; the pool names it twice and takes each once.
        var store = Store("a");
        long one = store.Save(Record(Picture(100), null!, "2026-11-10T10:00:00Z"));
        long two = store.Save(Record(Picture(200), null!, "2026-11-10T10:00:00Z"));
        Assert.Equal(DataExport.Identity(store.Get(one)!), DataExport.Identity(store.Get(two)!));
        var pool = SessionPools.Save(store, null, [one, two], SessionPools.Choices(pictures: true), DateTime.UtcNow);
        var opened = SessionPools.Open(store, pool);
        Assert.Equal([one, two], opened.SessionIds);
        Assert.Equal(6, opened.Composite.Shots.Count);

        store.Delete(two);
        Assert.Single(store.GetPool(pool.Id)!.Sheets);
        Assert.True(SessionPools.Open(store, store.GetPool(pool.Id)!).TooFew);
    }

    [Fact]
    public void ADatabaseOfVersionSixGainsThePoolsTableAndKeepsACopyFirst()
    {
        string path = Path.Combine(root, "old", "grouplab.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var store = SessionStore.Open(path);
        store.Save(Record(Picture(100), "aa", "2026-11-10T10:00:00Z"));
        using (var db = new SqliteConnection($"Data Source={path}"))
        {
            db.Open();
            using var command = db.CreateCommand();
            command.CommandText = "DROP TABLE pools; UPDATE meta SET value = '6' WHERE key = 'schema_version';";
            command.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
        var upgraded = SessionStore.Open(path);
        Assert.NotNull(upgraded.PreUpgradeCopy);
        Assert.Empty(upgraded.Pools());
        Assert.Single(upgraded.List());
        Assert.Equal(SessionStore.Schema, upgraded.CreatedSchema());
    }
}
