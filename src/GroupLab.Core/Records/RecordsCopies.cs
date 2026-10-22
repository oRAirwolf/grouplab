using System.Globalization;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Records;

/// <summary>One daily copy of the records: where it is, when it was made, how big it is, and how many sessions it holds.</summary>
public sealed record RecordsCopy(string Path, DateTime Made, long Bytes, int? Sessions);

/// <summary>What making a copy did: the copy where one was made, and a line that says so or says why not.</summary>
public sealed record CopyResult(RecordsCopy? Copy, string Said)
{
    public bool Made => Copy is not null;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 2.1, Alan's priority: "Keep a daily copy of your records". Once a day, at the first open, a
/// consistent copy of the database made while it is open (SQLite's own VACUUM INTO, as the copy before an upgrade is), the newest
/// <see cref="Kept"/> kept, older ones moved to GroupLab's trash and removed from it after <see cref="TrashDays"/> days. A copy that cannot be
/// finished, a full disk among them, leaves nothing half written and touches neither the records nor the copies already made.
/// </summary>
public sealed class RecordsCopies(string database, Func<DateTime> now)
{
    public const int Kept = 14;

    public const int TrashDays = 14;

    private const string Prefix = "grouplab-";
    private const string Stamp = "yyyy-MM-dd-HHmmss";

    /// <summary>The folder the copies are in, beside the database.</summary>
    public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(database))!, "copies");

    /// <summary>GroupLab's trash, where a copy past the newest <see cref="Kept"/> waits <see cref="TrashDays"/> days before it is removed.</summary>
    public string Trash { get; } = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(database))!, "trash", "copies");

    /// <summary>How a copy is written; a test puts a full disk here.</summary>
    internal Action<string, string> Write { get; set; } = VacuumInto;

    /// <summary>The copies, newest first.</summary>
    public IReadOnlyList<RecordsCopy> List(bool countSessions = false)
    {
        if (!Directory.Exists(Folder))
        {
            return [];
        }

        var copies = new List<RecordsCopy>();
        foreach (var path in Directory.EnumerateFiles(Folder, Prefix + "*.db"))
        {
            string stamp = System.IO.Path.GetFileNameWithoutExtension(path)[Prefix.Length..];
            if (DateTime.TryParseExact(stamp, Stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var made))
            {
                copies.Add(new RecordsCopy(path, made, new FileInfo(path).Length, countSessions ? CountSessions(path) : null));
            }
        }

        return [.. copies.OrderByDescending(c => c.Made)];
    }

    /// <summary>The newest copy, or null when none has been made.</summary>
    public RecordsCopy? Newest => List().FirstOrDefault();

    /// <summary>Whether today's copy is still to be made: none yet today, and records to copy.</summary>
    public bool Due() => File.Exists(database) && (Newest is not { } newest || newest.Made.Date < now().Date);

    /// <summary>Today's copy, at the first open of the day; nothing where it has been made.</summary>
    public CopyResult MakeIfDue() => Due() ? Make() : new CopyResult(null, "Today's copy is already made.");

    /// <summary>A copy now, as "Back up now" makes one, then the oldest past <see cref="Kept"/> moved to the trash.</summary>
    public CopyResult Make()
    {
        if (!File.Exists(database))
        {
            return new CopyResult(null, "There are no records to copy yet.");
        }

        var made = now();
        string path = System.IO.Path.Combine(Folder, Prefix + made.ToString(Stamp, CultureInfo.InvariantCulture) + ".db");
        for (int second = 1; File.Exists(path); second++)
        {
            path = System.IO.Path.Combine(Folder, Prefix + made.AddSeconds(second).ToString(Stamp, CultureInfo.InvariantCulture) + ".db");
        }

        string partial = path + ".partial";
        try
        {
            Directory.CreateDirectory(Folder);
            Write(database, partial);
            File.Move(partial, path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SqliteException)
        {
            // A full disk or a folder that cannot be written: nothing half written is left, and nothing else is touched.
            TryDelete(partial);
            return new CopyResult(null, "The copy could not be made, and nothing was changed: " + (e is SqliteException { SqliteErrorCode: 13 } ? "the disk is full." : e.Message));
        }

        Tidy();
        var copy = new RecordsCopy(path, DateTime.ParseExact(System.IO.Path.GetFileNameWithoutExtension(path)[Prefix.Length..], Stamp, CultureInfo.InvariantCulture), new FileInfo(path).Length, null);
        return new CopyResult(copy, "A copy of your records was made.");
    }

    /// <summary>Moves the copies past the newest <see cref="Kept"/> to the trash, and removes from it what has waited <see cref="TrashDays"/> days.</summary>
    public void Tidy()
    {
        foreach (var old in List().Skip(Kept))
        {
            try
            {
                Directory.CreateDirectory(Trash);
                // The day it was moved goes in front of its name, which is what the days in the trash are counted from.
                File.Move(old.Path, System.IO.Path.Combine(Trash, now().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + System.IO.Path.GetFileName(old.Path)), overwrite: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Left where it is, and tried again with the next copy.
            }
        }

        if (!Directory.Exists(Trash))
        {
            return;
        }

        foreach (var waiting in Directory.EnumerateFiles(Trash, "*-" + Prefix + "*.db"))
        {
            string day = System.IO.Path.GetFileName(waiting)[..8];
            if (DateTime.TryParseExact(day, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var moved) && (now().Date - moved).TotalDays >= TrashDays)
            {
                TryDelete(waiting);
            }
        }
    }

    /// <summary>
    /// Puts <paramref name="copy"/> in place of the records, after making a copy of the records as they are now so the restore can itself be
    /// undone. Nothing may have the database open meanwhile; GroupLab opens it afresh afterwards.
    /// </summary>
    public CopyResult Restore(RecordsCopy copy)
    {
        if (!File.Exists(copy.Path))
        {
            return new CopyResult(null, "That copy is no longer there, so nothing was changed.");
        }

        // The chosen copy is taken first, since the copy of the records made next may send the oldest to the trash.
        string incoming = database + ".restoring";
        try
        {
            File.Copy(copy.Path, incoming, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            TryDelete(incoming);
            return new CopyResult(null, "That copy could not be read, so nothing was changed: " + e.Message);
        }

        var before = Make();
        if (File.Exists(database) && !before.Made)
        {
            TryDelete(incoming);
            return new CopyResult(null, "Your records as they are now could not be copied first, so nothing was restored. " + before.Said);
        }

        try
        {
            SqliteConnection.ClearAllPools();
            File.Move(incoming, database, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            TryDelete(incoming);
            return new CopyResult(null, "The copy could not be put in place, and your records are as they were: " + e.Message);
        }

        return new CopyResult(copy, string.Create(CultureInfo.InvariantCulture, $"Your records are back as they were on {Words(copy.Made)}. Your records from before are kept as the newest copy."));
    }

    /// <summary>How many sessions a copy holds, read without changing it, or null where it cannot be read.</summary>
    public static int? CountSessions(string path)
    {
        try
        {
            using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            db.Open();
            using var command = db.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sessions";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        catch (SqliteException)
        {
            return null;
        }
    }

    /// <summary>A copy's day and time, as a person reads it: "22 October 2026 at 07:40".</summary>
    public static string Words(DateTime made) => made.ToString("d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    /// <summary>A size as a person reads it: "412 KB", "3.1 MB".</summary>
    public static string Size(long bytes) => bytes switch
    {
        < 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes} bytes"),
        < 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024.0:0} KB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024.0 * 1024):0.0} MB"),
    };

    private static void VacuumInto(string source, string into)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "VACUUM INTO $to";
        command.Parameters.AddWithValue("$to", into);
        command.ExecuteNonQuery();
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Left for the next tidy.
        }
    }
}
