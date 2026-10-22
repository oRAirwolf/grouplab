using GroupLab.Core.Records;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 2.1: a daily copy of the records, made while the records are open and being written, put back
/// in place with the records as they were kept first, the newest fourteen kept, and a full disk handled without losing anything.
/// </summary>
public sealed class RecordsCopiesTests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-copies-{Guid.NewGuid():N}")).FullName;
    private DateTime clock = new(2026, 10, 22, 7, 40, 0);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(folder, recursive: true);
    }

    private string Database => Path.Combine(folder, "grouplab.db");

    private RecordsCopies Copies() => new(Database, () => clock);

    private static SessionRecord Session(int n) => new(
        0, "2026-10-22T07:40:00Z", "2026-10-22", "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", "Bartlein 26", "Load " + n, 0.264,
        "{\"marking\":1}", 25, 0.5, 0.4, 0.6, null, new string('a', 64), null, null);

    [Fact]
    public async Task ACopyMadeWhileTheRecordsAreBeingWrittenIsWholeAndReadable()
    {
        var store = SessionStore.Open(Database);
        store.Save(Session(0));
        using var stop = new CancellationTokenSource();
        int saved = 0;
        var writing = Task.Run(() =>
        {
            int n = 1;
            while (!stop.IsCancellationRequested)
            {
                store.Save(Session(n++));
                Interlocked.Increment(ref saved);
            }

            return n;
        });

        // The copies start once the writing has, and the writing goes on through them.
        var until = DateTime.UtcNow.AddSeconds(30);
        while (Volatile.Read(ref saved) < 2 && DateTime.UtcNow < until)
        {
            await Task.Delay(10);
        }

        var copies = Copies();
        var made = new List<RecordsCopy>();
        for (int i = 0; i < 5; i++)
        {
            var result = copies.Make();
            Assert.True(result.Made, result.Said);
            made.Add(result.Copy!);
            clock = clock.AddMinutes(1);
        }

        stop.Cancel();
        int written = await writing;
        Assert.True(written > 1, "nothing was written while the copies were made");
        foreach (var copy in made)
        {
            using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = copy.Path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            db.Open();
            using var check = db.CreateCommand();
            check.CommandText = "PRAGMA integrity_check";
            Assert.Equal("ok", check.ExecuteScalar() as string);
            Assert.InRange(RecordsCopies.CountSessions(copy.Path) ?? -1, 1, written);
        }

        Assert.Empty(Directory.EnumerateFiles(copies.Folder, "*.partial"));
    }

    [Fact]
    public void ACopyIsMadeOnceADayAtTheFirstOpen()
    {
        SessionStore.Open(Database).Save(Session(1));
        var copies = Copies();
        Assert.True(copies.Due());
        Assert.True(copies.MakeIfDue().Made);
        clock = clock.AddHours(5);
        Assert.False(copies.Due());
        Assert.False(copies.MakeIfDue().Made);
        clock = clock.AddDays(1);
        Assert.True(copies.MakeIfDue().Made);
        Assert.Equal(2, copies.List().Count);
    }

    [Fact]
    public void NoRecordsYetMeansNoCopyAndNoFolder()
    {
        var copies = Copies();
        Assert.False(copies.Due());
        Assert.False(copies.Make().Made);
        Assert.False(Directory.Exists(copies.Folder));
    }

    [Fact]
    public void RestoringPutsTheCopyBackAndKeepsTheRecordsAsTheyWereFirst()
    {
        var store = SessionStore.Open(Database);
        store.Save(Session(1));
        store.Save(Session(2));
        var copies = Copies();
        var two = copies.Make().Copy!;
        clock = clock.AddDays(1);
        store.Save(Session(3));
        Assert.Equal(3, store.List().Count);

        clock = clock.AddMinutes(5);
        var restored = copies.Restore(copies.List(countSessions: true).Single(c => c.Path == two.Path));
        Assert.True(restored.Made, restored.Said);

        Assert.Equal(2, SessionStore.Open(Database).List().Count);
        var listed = copies.List(countSessions: true);
        Assert.Equal(2, listed.Count);
        Assert.Equal(3, listed[0].Sessions);
        Assert.Equal(2, listed[1].Sessions);

        // And the restore itself is undone the same way.
        Assert.True(copies.Restore(listed[0]).Made);
        Assert.Equal(3, SessionStore.Open(Database).List().Count);
    }

    [Fact]
    public void TheNewestFourteenAreKeptAndTheRestWaitInTheTrashFourteenDays()
    {
        SessionStore.Open(Database).Save(Session(1));
        var copies = Copies();
        var start = clock;
        for (int day = 0; day < RecordsCopies.Kept + 2; day++)
        {
            clock = start.AddDays(day);
            Assert.True(copies.MakeIfDue().Made);
        }

        var kept = copies.List();
        Assert.Equal(RecordsCopies.Kept, kept.Count);
        Assert.Equal(start.AddDays(RecordsCopies.Kept + 1).Date, kept[0].Made.Date);
        Assert.Equal(start.AddDays(2).Date, kept[^1].Made.Date);
        Assert.Equal(2, Directory.GetFiles(copies.Trash).Length);

        // The first went to the trash on the day the fifteenth copy was made, and is removed fourteen days after that.
        clock = start.AddDays(RecordsCopies.Kept + RecordsCopies.TrashDays - 1);
        copies.Tidy();
        Assert.Equal(2, Directory.GetFiles(copies.Trash).Length);
        clock = clock.AddDays(1);
        copies.Tidy();
        Assert.Single(Directory.GetFiles(copies.Trash));
        clock = clock.AddDays(1);
        copies.Tidy();
        Assert.Empty(Directory.GetFiles(copies.Trash));
        Assert.Equal(RecordsCopies.Kept, copies.List().Count);
    }

    [Fact]
    public void AFullDiskLosesNothing()
    {
        SessionStore.Open(Database).Save(Session(1));
        var copies = Copies();
        var first = copies.Make().Copy!;
        byte[] records = File.ReadAllBytes(Database);
        byte[] kept = File.ReadAllBytes(first.Path);

        clock = clock.AddDays(1);
        copies.Write = (_, into) =>
        {
            File.WriteAllBytes(into, new byte[100]);
            throw new IOException("There is not enough space on the disk.");
        };
        var failed = copies.Make();
        Assert.False(failed.Made);
        Assert.Contains("nothing was changed", failed.Said, StringComparison.Ordinal);
        Assert.Contains("not enough space", failed.Said, StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFiles(copies.Folder, "*.partial"));
        Assert.Equal(first.Path, copies.List().Single().Path);
        Assert.Equal(kept, File.ReadAllBytes(first.Path));
        Assert.Equal(records, File.ReadAllBytes(Database));

        // A restore that cannot copy the records first restores nothing.
        var restore = copies.Restore(first);
        Assert.False(restore.Made);
        Assert.Equal(records, File.ReadAllBytes(Database));
        Assert.False(File.Exists(Database + ".restoring"));
    }

    [Fact]
    public void ACopyIsNamedAndSizedAsAPersonReadsIt()
    {
        Assert.Equal("22 October 2026 at 07:40", RecordsCopies.Words(clock));
        Assert.Equal("412 KB", RecordsCopies.Size(412 * 1024));
        Assert.Equal("3.1 MB", RecordsCopies.Size((long)(3.1 * 1024 * 1024)));
    }
}
