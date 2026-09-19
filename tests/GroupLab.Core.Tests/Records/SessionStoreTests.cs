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

    public void Dispose() => Directory.Delete(folder, recursive: true);

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
        Assert.Throws<InvalidDataException>(() => New("other.db").Import(export.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal)));
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

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
