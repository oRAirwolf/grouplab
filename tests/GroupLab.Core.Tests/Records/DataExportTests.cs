using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307: everything in one file that any GroupLab reads, merged on import without overwriting or duplicating, with
/// the conflicts listed first. Round trip, an older file, a damaged one, and a large library. Run with nothing beside it: the large library
/// measures the memory the import holds, and another test allocating at the same time is counted too (Windows, c1918e19, 2026-09-30).
/// </summary>
[Collection(MeasuredAloneCollection.Name)]
public class DataExportTests
{
    private static string Folder() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-data-{Guid.NewGuid():N}")).FullName;

    private static SessionRecord Session(int n, byte[]? proof = null) => new(0, $"2026-09-{10 + (n % 20):00}T10:{n % 60:00}:{n / 60 % 60:00}Z", "2026-09-20", $"Sheet {n}", "GL-CF25-LTR", "{}",
        3600, "Tikka", null, "H4350", 0.308, $"{{\"shots\":{n}}}", 5, 0.4 + (n * 0.001), 0.3, 0.6, @"C:\Users\someone\Pictures\target.jpg", $"sha{n}", proof ?? [1, 2, 3, (byte)n], "image/jpeg");

    private static SessionStore Filled(string folder, int count = 3)
    {
        var store = SessionStore.Open(Path.Combine(folder, "grouplab.db"));
        store.SaveBook(RecordBook.Empty.With(new Rifle("Tikka", 0.1, AngularUnit.Mrad) { SightHeightInches = 1.75 }).With(new Load("H4350", "41.5 gr")));
        for (int i = 0; i < count; i++)
        {
            long id = store.Save(Session(i));
            long s = store.AddChronographString(id, "LabRadar", null, [2710, 2715, 2705]);
            store.MapShot(new ShotVelocity(id, 1, s, 2));
        }

        return store;
    }

    private static byte[] Export(SessionStore store, IReadOnlyList<ExportedSheet>? sheets = null, JsonObject? settings = null)
    {
        using var stream = new MemoryStream();
        DataExport.Write(stream, store, sheets ?? [], settings ?? [], "test", new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc));
        return stream.ToArray();
    }

    private static DataFile Read(byte[] bytes) => DataExport.Read(new MemoryStream(bytes));

    private static void Delete(string folder)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void DesktopToPhoneToDesktopComesBackTheSame()
    {
        string a = Folder(), b = Folder(), c = Folder();
        try
        {
            var desktop = Filled(a);
            var sheets = new List<ExportedSheet> { new("my-sheet.gltd.json", "{\"name\":\"Mine\"}") };
            var settings = new JsonObject { ["printers"] = new JsonArray(new JsonObject { ["name"] = "Brother", ["scale"] = 0.998 }), ["angular"] = "Mrad", ["theme"] = "Dark" };
            byte[] first = Export(desktop, sheets, settings);
            Assert.DoesNotContain("someone", Encoding.UTF8.GetString(first), StringComparison.Ordinal);

            // Onto an empty phone: everything is new.
            var phone = SessionStore.Open(Path.Combine(b, "sessions.db"));
            var plan = DataExport.Plan(Read(first), phone, [], []);
            Assert.Equal(3, plan.NewSessions.Count);
            Assert.Empty(plan.Conflicts);
            Assert.Equal(2, plan.NewSettingsItems);
            Assert.False(plan.NewSettings.ContainsKey("theme"));
            DataExport.Apply(plan, phone);

            // And back to an empty computer from the phone's own export.
            byte[] second = Export(phone, sheets, settings);
            var again = SessionStore.Open(Path.Combine(c, "grouplab.db"));
            DataExport.Apply(DataExport.Plan(Read(second), again, [], []), again);

            var original = desktop.List().Select(s => desktop.Get(s.Id)!).OrderBy(s => s.CreatedUtc).ToList();
            var back = again.List().Select(s => again.Get(s.Id)!).OrderBy(s => s.CreatedUtc).ToList();
            Assert.Equal(original.Count, back.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i] with { Id = 0, ImagePath = null, ProofImage = null }, back[i] with { Id = 0, ImagePath = null, ProofImage = null });
                Assert.Equal(original[i].ProofImage, back[i].ProofImage);
                Assert.Equal([2710, 2715, 2705], again.ChronographStrings(back[i].Id).Single().VelocitiesFps);
                Assert.Equal(2, again.ShotVelocities(back[i].Id).Single().Ordinal);
            }

            Assert.Equal(desktop.LoadBook(), again.LoadBook() with { Rifles = [.. again.LoadBook().Rifles], Loads = [.. again.LoadBook().Loads] }, new BookComparer());
        }
        finally
        {
            Delete(a);
            Delete(b);
            Delete(c);
        }
    }

    private sealed class BookComparer : IEqualityComparer<RecordBook>
    {
        public bool Equals(RecordBook? x, RecordBook? y) => x!.Rifles.SequenceEqual(y!.Rifles) && x.Barrels.SequenceEqual(y.Barrels) && x.Loads.SequenceEqual(y.Loads);

        public int GetHashCode(RecordBook obj) => 0;
    }

    [Fact]
    public void ImportingTwiceAddsNothingAndADifferenceIsListedNotOverwritten()
    {
        string a = Folder();
        try
        {
            var store = Filled(a);
            byte[] bytes = Export(store);
            var same = DataExport.Plan(Read(bytes), store, [], []);
            Assert.Equal(0, same.Adds);
            Assert.Equal(3, same.SameSessions);
            Assert.Empty(same.Conflicts);

            // A session edited here since, and a rifle changed: both listed, both kept as they are here.
            var first = store.Get(store.List().OrderBy(s => s.Id).First().Id)!;
            store.Save(first with { MarkingJson = "{\"shots\":99}" });
            store.SaveBook(store.LoadBook().With(new Rifle("Tikka", 0.25, AngularUnit.Moa)));
            var plan = DataExport.Plan(Read(bytes), store, [], []);
            Assert.Equal(2, plan.Conflicts.Count);
            Assert.Contains(plan.Conflicts, c => c.StartsWith("Rifle Tikka", StringComparison.Ordinal));
            DataExport.Apply(plan, store);
            Assert.Equal("{\"shots\":99}", store.Get(first.Id)!.MarkingJson);
            Assert.Equal(0.25, store.LoadBook().FindRifle("Tikka")!.ClickValue);
            Assert.Equal(3, store.List().Count);
        }
        finally
        {
            Delete(a);
        }
    }

    [Fact]
    public void TwoSessionsWithOneIdentityAreNeitherAConflictNorAnError()
    {
        // The same picture read twice in one second, with a different load the second time: both here, both in the file.
        string a = Folder(), b = Folder();
        try
        {
            var store = SessionStore.Open(Path.Combine(a, "grouplab.db"));
            store.Save(Session(1));
            store.Save(Session(1) with { Load = "Varget" });
            byte[] bytes = Export(store);
            var again = DataExport.Plan(Read(bytes), store, [], []);
            Assert.Equal(0, again.Adds);
            Assert.Empty(again.Conflicts);

            var elsewhere = SessionStore.Open(Path.Combine(b, "grouplab.db"));
            var plan = DataExport.Plan(Read(bytes), elsewhere, [], []);
            Assert.Equal(2, plan.NewSessions.Count);
        }
        finally
        {
            Delete(a);
            Delete(b);
        }
    }

    [Fact]
    public void AnOlderFileIsReadAndANewerOrDamagedOneIsRefusedPlainly()
    {
        // Version 1 as the first builds write it may carry nothing but sessions.
        var older = DataExport.Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"format\":\"grouplab-data\",\"version\":1,\"sessions\":[]}")));
        Assert.Empty(older.Sessions);
        Assert.Empty(older.Book.Rifles);

        var newer = Assert.Throws<InvalidDataException>(() => DataExport.Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"format\":\"grouplab-data\",\"version\":9}"))));
        Assert.Contains("newer GroupLab", newer.Message, StringComparison.Ordinal);
        var damaged = Assert.Throws<InvalidDataException>(() => DataExport.Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"format\":\"grouplab-data\",\"version\":1,\"sessions\":[{\"sess"))));
        Assert.Contains("damaged", damaged.Message, StringComparison.Ordinal);
        var other = Assert.Throws<InvalidDataException>(() => DataExport.Read(new MemoryStream(Encoding.UTF8.GetBytes("{\"hello\":1}"))));
        Assert.Contains("not a GroupLab data file", other.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALargeLibraryGoesAndComesBackInReasonableTimeAndMemory()
    {
        string a = Folder(), b = Folder();
        try
        {
            var store = SessionStore.Open(Path.Combine(a, "grouplab.db"));
            var proof = new byte[60_000];
            new Random(307).NextBytes(proof);
            for (int i = 0; i < 300; i++)
            {
                store.Save(Session(i, proof));
            }

            string path = Path.Combine(a, "all.grouplab");
            var clock = Stopwatch.StartNew();
            long before = GC.GetTotalMemory(true);
            using (var file = File.Create(path))
            {
                DataExport.Write(file, store, [], [], "test", DateTime.UtcNow);
            }

            var target = SessionStore.Open(Path.Combine(b, "grouplab.db"));
            using (var file = File.OpenRead(path))
            {
                DataExport.Apply(DataExport.Plan(DataExport.Read(file), target, [], []), target);
            }

            clock.Stop();
            Assert.Equal(300, target.List().Count);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(60), $"300 sessions took {clock.Elapsed.TotalSeconds:0} s");
            Assert.True(new FileInfo(path).Length > 300 * 60_000, "every picture is in the file");
            // Collected first: garbage the runner has not swept yet is not memory the import holds, and Windows sweeps later than Linux.
            Assert.True(GC.GetTotalMemory(true) - before < 400_000_000, "the import held far more than the file in memory");
        }
        finally
        {
            Delete(a);
            Delete(b);
        }
    }
}
