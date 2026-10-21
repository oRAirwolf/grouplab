using System.Text.RegularExpressions;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112 section 1 and DESIGN.md section 15: one SQLite database, a documented schema the code is held to, full JSON
/// export and import that round-trip exactly, the old record file migrated and kept, and room for chronograph strings and their mapping.
/// </summary>
public sealed partial class SessionStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"grouplab-store-{Guid.NewGuid():N}");

    public SessionStoreTests() => Directory.CreateDirectory(folder);

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(folder);

    private SessionStore New(string name = "grouplab.db", string? legacy = null) => SessionStore.Open(Path.Combine(folder, name), legacy);

    private static readonly RecordBook Book = RecordBook.Empty
        .With(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100, TwistInches = 8, TwistDirection = 1 })
        .With(new Rifle("Old Mauser", 0.1, AngularUnit.Mrad))
        .With(new Barrel("Bartlein 26", "Tikka T3x", 412))
        .With(new Load("H4350 41.5", "140 ELD-M, Lapua brass")
        {
            MuzzleVelocityFps = 2710, MuzzleVelocitySdFps = 9.5, BallisticCoefficient = 0.326, DragModel = DragModel.G7,
            BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140, BulletLengthInches = 1.39, BulletDiameterInches = 0.264,
        });

    private static SessionRecord Session(string created, string rifle, string load, double meanRadius) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, rifle, "Bartlein 26", load, 0.264,
        "{\"marking\":1}", 25, meanRadius, meanRadius * 0.8, meanRadius * 1.3, "C:\\scans\\sheet.png", new string('a', 64), [1, 2, 3, 250], "image/jpeg");

    private static string Normal(string sql) => Whitespace().Replace(sql.Trim().TrimEnd(';'), " ");

    /// <summary>docs/SESSION-SCHEMA.md gives exactly the statements the code runs, and the database SQLite keeps is made of exactly those.</summary>
    [Fact]
    public void TheSchemaDocumentIsTheSchemaTheCodeCreates()
    {
        string document = File.ReadAllText(Repo.PathTo("docs", "SESSION-SCHEMA.md"));
        Assert.Contains($"**Schema version {SessionStore.SchemaVersion}.**", document, StringComparison.Ordinal);
        string block = document[(document.IndexOf("```sql", StringComparison.Ordinal) + 6)..];
        block = block[..block.IndexOf("```", StringComparison.Ordinal)];
        var documented = block.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Normal).ToList();
        Assert.Equal(SessionStore.Schema.Select(Normal), documented);
        Assert.Equal(documented, New().CreatedSchema().Select(Normal));
    }

    [Fact]
    public void RecordsAndSessionsRoundTripThroughTheDatabase()
    {
        var store = New();
        store.SaveBook(Book);
        var book = store.LoadBook();
        Assert.Equal(Book.Rifles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase), book.Rifles);
        Assert.Equal(Book.Barrels, book.Barrels);
        Assert.Equal(Book.Loads, book.Loads);

        long first = store.Save(Session("2026-09-20T15:00:00Z", "Tikka T3x", "H4350 41.5", 0.12));
        long second = store.Save(Session("2026-09-21T15:00:00Z", "Old Mauser", "Factory 150", 0.3));
        var got = store.Get(first)!;
        Assert.Equal([1, 2, 3, 250], got.ProofImage);
        Assert.Equal(Session("2026-09-20T15:00:00Z", "Tikka T3x", "H4350 41.5", 0.12) with { Id = first, ProofImage = got.ProofImage }, got);

        // Newest first, and filtered by rifle or by load.
        Assert.Equal([second, first], store.List().Select(s => s.Id));
        Assert.Equal([first], store.List(rifle: "tikka t3x").Select(s => s.Id));
        Assert.Equal([second], store.List(load: "Factory 150").Select(s => s.Id));
        Assert.Equal(2, store.CountUsing("GL-TEST-0000"));

        // Saving with an id replaces that session, as a second Accept and analyse does.
        store.Save(got with { ShotCount = 24 });
        Assert.Equal(24, store.Get(first)!.ShotCount);
        Assert.True(store.Delete(first));
        Assert.Null(store.Get(first));
        Assert.Equal([second], store.List().Select(s => s.Id));
    }

    /// <summary>
    /// DESIGN.md section 15: the chronograph's readings are their own ordered list, and which shot is which reading is an explicit mapping. The
    /// schema holds both now, though import is Phase 5, and deleting a session takes its strings and mapping with it.
    /// </summary>
    [Fact]
    public void AChronographStringIsItsOwnListWithAnExplicitMapping()
    {
        var store = New();
        long session = store.Save(Session("2026-09-20T15:00:00Z", "Tikka T3x", "H4350 41.5", 0.12));
        long chronograph = store.AddChronographString(session, "Garmin Xero C1", "2026-09-20T14:55:00Z", [2705.2, 2711.0, 2698.4]);
        store.MapShot(new ShotVelocity(session, 7, chronograph, 2));
        Assert.Equal([2705.2, 2711.0, 2698.4], store.ChronographStrings(session).Single().VelocitiesFps);
        Assert.Equal(new ShotVelocity(session, 7, chronograph, 2), store.ShotVelocities(session).Single());
        store.Delete(session);
        Assert.Empty(store.ChronographStrings(session));
        Assert.Empty(store.ShotVelocities(session));
    }

    /// <summary>
    /// Entry 416, found while building it: saving a session again replaced its row, and the delete under the replace took the session's
    /// chronograph readings and their pairing with shots with it, so a sheet accepted a second time lost its readings. Its row is updated.
    /// </summary>
    [Fact]
    public void SavingASessionAgainKeepsItsReadings()
    {
        var store = New();
        var first = Session("2026-09-20T15:00:00Z", "Tikka T3x", "H4350 41.5", 0.12);
        long session = store.Save(first);
        long chronograph = store.AddChronographString(session, "Garmin Xero C1", null, [2705.2, 2711.0]);
        store.MapShot(new ShotVelocity(session, 7, chronograph, 2));

        Assert.Equal(session, store.Save(first with { Id = session, MeanRadiusInches = 0.2 }));
        Assert.Equal(0.2, store.Get(session)!.MeanRadiusInches);
        Assert.Equal([2705.2, 2711.0], store.ChronographStrings(session).Single().VelocitiesFps);
        Assert.Equal(new ShotVelocity(session, 7, chronograph, 2), store.ShotVelocities(session).Single());
    }

    /// <summary>Full JSON export and import, section 15: an export read into an empty database exports again byte for byte the same.</summary>
    [Fact]
    public void TheExportRoundTripsExactly()
    {
        var store = New();
        store.SaveBook(Book);
        long session = store.Save(Session("2026-09-20T15:00:00Z", "Tikka T3x", "H4350 41.5", 0.123456789012345));
        store.Save(Session("2026-09-21T15:00:00Z", "Old Mauser", "Factory 150", 0.3) with { ProofImage = null, ProofImageType = null, DistanceInches = null });
        long chronograph = store.AddChronographString(session, "Garmin Xero C1", null, [2705.25, 2711.0]);
        store.MapShot(new ShotVelocity(session, 3, chronograph, 1));
        string export = store.Export();

        var copy = New("copy.db");
        copy.Import(export);
        Assert.Equal(export, copy.Export());
        Assert.Throws<InvalidOperationException>(() => copy.Import(export));
        string newer = export.Replace($"\"schemaVersion\": {SessionStore.SchemaVersion}", $"\"schemaVersion\": {SessionStore.SchemaVersion + 1}", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => New("other.db").Import(newer));
    }

    /// <summary>The record file from before the database is read in on the first open and kept, renamed, as a backup; the second open leaves it alone.</summary>
    [Fact]
    public void TheOldRecordFileMigratesOnceAndIsKept()
    {
        string legacy = Path.Combine(folder, "records.json");
        File.WriteAllText(legacy, RecordBook.Empty.With(new Rifle("Tikka T3x", 0.25, AngularUnit.Moa)).With(new Load("H4350 41.5", null)).Write());
        var store = New(legacy: legacy);
        Assert.Equal("Tikka T3x", store.LoadBook().Rifles.Single().Name);
        Assert.False(File.Exists(legacy));
        Assert.True(File.Exists(Path.Combine(folder, "records.pre-database.json")));

        store.SaveBook(RecordBook.Empty);
        File.WriteAllText(legacy, RecordBook.Empty.With(new Rifle("Stray", 1, AngularUnit.Moa)).Write());
        Assert.Empty(New(legacy: legacy).LoadBook().Rifles);
    }

    /// <summary>
    /// Entry 115 section 3: a database written by the GroupLab before this one is brought up to this schema when it is opened, in one
    /// transaction with its version, and everything in it is still there. A database of a newer version is refused with its number.
    /// </summary>
    [Fact]
    public void ADatabaseOfTheVersionBeforeIsBroughtUpToThisOne()
    {
        // Version 1's schema is version 2's without the column version 2 adds.
        string path = Path.Combine(folder, "old.db");
        Build(path, [.. VersionTwo.Select(s => s.Replace(", muzzle_velocity_sd_from TEXT)", ")", StringComparison.Ordinal)),
            "INSERT INTO meta (key, value) VALUES ('schema_version', '1')", "INSERT INTO loads (name, muzzle_velocity_fps) VALUES ('H4350 41.5', 2710)"]);

        var store = SessionStore.Open(path);
        var load = Assert.Single(store.LoadBook().Loads);
        Assert.Equal(("H4350 41.5", 2710.0), (load.Name, load.MuzzleVelocityFps));
        Assert.Null(load.MuzzleVelocitySdFrom);
        Assert.Equal(SessionStore.Schema.Select(Normal), store.CreatedSchema().Select(Normal));

        // The new column holds what it is for, and the version is this one now.
        store.SaveBook(RecordBook.Empty.With(load with { MuzzleVelocitySdFps = 9.5, MuzzleVelocitySdFrom = "24 readings, 20 September 2026" }));
        Assert.Equal("24 readings, 20 September 2026", SessionStore.Open(path).LoadBook().Loads.Single().MuzzleVelocitySdFrom);
    }

    /// <summary>Version 2's schema: this one with the rifles, barrels and loads tables as they were before entry 407 section 5.</summary>
    // Version 2 had none of synchronization's tables, which version 6 added.
    private static IEnumerable<string> VersionTwo => SessionStore.Schema.Where(s => !s.Contains("TABLE sync_", StringComparison.Ordinal)).Select(s =>
        s.StartsWith("CREATE TABLE rifles", StringComparison.Ordinal) ? "CREATE TABLE rifles (name TEXT PRIMARY KEY COLLATE NOCASE, click_value REAL NOT NULL, click_unit TEXT NOT NULL, sight_height_in REAL, zero_distance_yd REAL, twist_in REAL, twist_direction INTEGER)"
        : s.StartsWith("CREATE TABLE barrels", StringComparison.Ordinal) ? "CREATE TABLE barrels (name TEXT PRIMARY KEY COLLATE NOCASE, rifle TEXT, rounds INTEGER NOT NULL)"
        : s.StartsWith("CREATE TABLE loads", StringComparison.Ordinal) ? "CREATE TABLE loads (name TEXT PRIMARY KEY COLLATE NOCASE, components TEXT, muzzle_velocity_fps REAL, muzzle_velocity_sd_fps REAL, ballistic_coefficient REAL, drag_model TEXT, bc_reference TEXT, bullet_weight_gr REAL, bullet_length_in REAL, bullet_diameter_in REAL, muzzle_velocity_sd_from TEXT)"
        : s);

    private static void Build(string path, IEnumerable<string> statements)
    {
        using var db = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=false");
        db.Open();
        foreach (string statement in statements)
        {
            using var command = db.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 407 section 5.5: a version 2 database is copied beside itself first, then brought up, and every barrel goes
    /// under a rifle: one named by its sessions under that rifle, one nobody can place under "Barrels not yet on a rifle", and a rifle's own
    /// length and twist become its first barrel. Every old session still names the same rifle and barrel, and the counts say what moved.
    /// </summary>
    [Fact]
    public void AVersionTwoDatabaseIsCopiedThenEveryBarrelGoesUnderARifle()
    {
        string path = Path.Combine(folder, "two.db");
        Build(path, [.. VersionTwo,
            "INSERT INTO meta (key, value) VALUES ('schema_version', '2')",
            "INSERT INTO rifles (name, click_value, click_unit, twist_in, twist_direction) VALUES ('Tikka T3x', 0.25, 'Moa', 8, 1)",
            "INSERT INTO rifles (name, click_value, click_unit) VALUES ('Old Mauser', 0.1, 'Mrad')",
            "INSERT INTO barrels (name, rifle, rounds) VALUES ('Bartlein 26', NULL, 412)",
            "INSERT INTO barrels (name, rifle, rounds) VALUES ('Spare', NULL, 30)",
            "INSERT INTO sessions (id, created_utc, sheet_name, rifle, barrel, marking_json, shot_count) VALUES (1, '2026-10-01T10:00:00Z', 'Sheet', 'Old Mauser', 'Bartlein 26', '{}', 5)",
            "INSERT INTO sessions (id, created_utc, sheet_name, rifle, barrel, marking_json, shot_count) VALUES (2, '2026-10-02T10:00:00Z', 'Sheet', 'Old Mauser', 'Bartlein 26', '{}', 5)",
            "INSERT INTO sessions (id, created_utc, sheet_name, rifle, barrel, marking_json, shot_count) VALUES (3, '2026-10-03T10:00:00Z', 'Sheet', 'Tikka T3x', 'Spare', '{}', 5)",
            "INSERT INTO sessions (id, created_utc, sheet_name, rifle, barrel, marking_json, shot_count) VALUES (4, '2026-10-04T10:00:00Z', 'Sheet', 'Old Mauser', 'Spare', '{}', 5)"]);

        var store = SessionStore.Open(path);
        Assert.NotNull(store.PreUpgradeCopy);
        Assert.True(File.Exists(store.PreUpgradeCopy));
        Assert.Equal("two.pre-v2.db", Path.GetFileName(store.PreUpgradeCopy));
        Assert.Equal(SessionStore.Schema.Select(Normal), store.CreatedSchema().Select(Normal));

        var book = store.LoadBook();
        Assert.Equal("Old Mauser", book.FindBarrel("Bartlein 26")!.Rifle);
        Assert.Equal(RecordBook.NotYetOnARifle, book.FindBarrel("Spare")!.Rifle);
        Assert.Equal(412, book.FindBarrel("Bartlein 26")!.Rounds);
        var first = book.FindBarrel("Tikka T3x barrel")!;
        Assert.Equal(("Tikka T3x", 8.0, 1), (first.Rifle, first.TwistInches!.Value, first.TwistDirection!.Value));
        Assert.Equal("Tikka T3x barrel", book.FindRifle("Tikka T3x")!.BarrelOn);
        Assert.Equal("Bartlein 26", book.FindRifle("Old Mauser")!.BarrelOn);
        Assert.All(book.Barrels, b => Assert.NotNull(book.FindRifle(b.Rifle)));

        // Every old session names the same rifle and barrel as before.
        Assert.Equal([("Old Mauser", "Bartlein 26"), ("Old Mauser", "Bartlein 26"), ("Tikka T3x", "Spare"), ("Old Mauser", "Spare")],
            Enumerable.Range(1, 4).Select(id => store.Get(id)!).Select(s => (s.Rifle, s.Barrel)));

        Assert.Equal(new BarrelMove(2, 3, 2, 3, 2, 0, 1, 1, 1), store.BarrelMove);
        Assert.StartsWith("rifles 2 to 3, barrels 2 to 3, barrels with no rifle 2 to 0", store.BarrelMoveSaid(), StringComparison.Ordinal);

        // Opening it again moves nothing and copies nothing.
        var again = SessionStore.Open(path);
        Assert.Null(again.PreUpgradeCopy);
        Assert.Null(again.BarrelMove);
        Assert.Equal(store.LoadBook().Barrels.Count, again.LoadBook().Barrels.Count);
    }

    /// <summary>Entry 407 section 5: a barrel's length and twist, and the other fields the Equipment screen has, now reach the database.</summary>
    [Fact]
    public void EveryEquipmentFieldReachesTheDatabase()
    {
        var store = New();
        var rifle = new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { Cartridge = "6.5 Creedmoor", Scope = "Vortex", Stock = "KRG", Notes = "zeroed in May", Manufacturer = "Tikka", BarrelOn = "Bartlein 26" };
        var barrel = new Barrel("Bartlein 26", "Tikka T3x", 412) { LengthInches = 26, TwistInches = 8, TwistDirection = 1, Installed = new DateOnly(2025, 3, 14), Notes = "fluted" };
        var load = new Load("H4350 41.5", null) { BulletName = "140 ELD-M", Powder = "H4350", PowderChargeGrains = 41.5, Primer = "CCI 200", OverallLengthInches = 2.8, BaseToOgiveInches = 2.2, BrassManufacturer = "Lapua", BrassCartridge = "6.5 CM", Notes = "best" };
        store.SaveBook(RecordBook.Empty.With(rifle).With(barrel).With(load));
        var read = SessionStore.Open(store.Path).LoadBook();
        Assert.Equal(barrel, read.Barrels.Single());
        Assert.Equal(load, read.Loads.Single());
        var back = read.Rifles.Single();
        Assert.Equal(("6.5 Creedmoor", "Vortex", "KRG", "Bartlein 26", 26.0, 8.0), (back.Cartridge, back.Scope, back.Stock, back.BarrelOn, back.BarrelLengthInches!.Value, back.TwistInches!.Value));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
