using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495, question 113 answered (b): a pooled set of sheets as a record of its own, schema 7. A pool is never a
/// session. It keeps which sheets it pools by their identity across devices (<see cref="DataExport.Identity"/>), in the order they were
/// pooled, and how they were pooled (<see cref="PoolRecord.Choices"/>), never a copy of their shots, so its figures are worked out again from
/// the sheets as they are now, and fixing a hole on one sheet changes the pool too. <see cref="PoolRecord.Key"/> names it on every device.
/// </summary>
public sealed record PoolRecord(long Id, string Key, string CreatedUtc, IReadOnlyList<string> Sheets, string? Choices)
{
    /// <summary>How a set of sheets from Made for your optic is pooled: each sheet by the number its codes give it.</summary>
    public const string AsSet = "set";

    /// <summary>How pictures with no set numbers are pooled: each its own sheet, in the order given (entries 423 and 460).</summary>
    public const string AsPictures = "pictures";

    /// <summary>Whether the pool reads its sheets as pictures in order, rather than as the sheets of one set.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Pictures => Choice("as") == AsPictures;

    /// <summary>The choices a pool is made with, as its record keeps them.</summary>
    public static string ChoicesFor(bool pictures) => new System.Text.Json.Nodes.JsonObject { ["as"] = pictures ? AsPictures : AsSet }.ToJsonString();

    private string? Choice(string name)
    {
        try
        {
            return Choices is { } json && System.Text.Json.Nodes.JsonNode.Parse(json) is System.Text.Json.Nodes.JsonObject o ? (string?)o[name] : null;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Whether two records are the same pool with the same contents, ids aside.</summary>
    public bool SameAs(PoolRecord other) =>
        other is not null && Key == other.Key && CreatedUtc == other.CreatedUtc && Choices == other.Choices && Sheets.SequenceEqual(other.Sheets, StringComparer.Ordinal);
}

public sealed partial class SessionStore
{
    /// <summary>Saves a pool: a new one (id 0) is added, one already here is updated in place. Returns its id.</summary>
    public long SavePool(PoolRecord pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        using var db = Connect();
        return WritePool(db, pool);
    }

    /// <summary>Every pool, newest first by when it was made.</summary>
    public IReadOnlyList<PoolRecord> Pools()
    {
        using var db = Connect();
        return [.. ReadPools(db).OrderByDescending(p => p.CreatedUtc, StringComparer.Ordinal).ThenByDescending(p => p.Id)];
    }

    public PoolRecord? GetPool(long id)
    {
        using var db = Connect();
        return ReadPools(db, id).FirstOrDefault();
    }

    public PoolRecord? PoolByKey(string key)
    {
        using var db = Connect();
        return ReadPools(db).FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Deletes a pool, and only the pool: its sheets stay where they are.</summary>
    public bool DeletePool(long id)
    {
        using var db = Connect();
        using var command = Command(db, "DELETE FROM pools WHERE id = $id", [("$id", id)]);
        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>The pools that use a session, by its identity.</summary>
    public IReadOnlyList<PoolRecord> PoolsUsing(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string identity = DataExport.Identity(session);
        return [.. Pools().Where(p => p.Sheets.Contains(identity, StringComparer.Ordinal))];
    }

    /// <summary>
    /// Each session's identity across devices, and its ids here, oldest first: two can share one (the same picture read twice in one second,
    /// or two sheets with no picture saved in the same second).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<long>> SessionIdentities()
    {
        using var db = Connect();
        using var command = Command(db, "SELECT id, created_utc, sheet_name, definition_id, image_sha256 FROM sessions ORDER BY id", []);
        using var r = command.ExecuteReader();
        var all = new Dictionary<string, List<long>>(StringComparer.Ordinal);
        while (r.Read())
        {
            string identity = $"{r.GetString(1)}|{r.GetString(2)}|{Text(r, 3)}|{Text(r, 4)}";
            (all.TryGetValue(identity, out var ids) ? ids : all[identity] = []).Add(r.GetInt64(0));
        }

        return all.ToDictionary(p => p.Key, p => (IReadOnlyList<long>)p.Value, StringComparer.Ordinal);
    }

    /// <summary>A pool another device changed, written here with its revision in one transaction, after the pool as it was is kept.</summary>
    public long ApplySyncedPool(PoolRecord incoming, SyncEntry entry, (string Payload, string Why, string Utc)? keep)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        if (keep is { } k)
        {
            WriteKept(db, entry.Kind, entry.Key, k.Payload, k.Why, k.Utc);
        }

        long? here = ReadPools(db).FirstOrDefault(p => string.Equals(p.Key, incoming.Key, StringComparison.OrdinalIgnoreCase))?.Id;
        long id = WritePool(db, incoming with { Id = here ?? 0 });
        WriteEntry(db, entry);
        transaction.Commit();
        return id;
    }

    /// <summary>A pool another device took away, deleted here with its revision in one transaction, after it is kept as it was.</summary>
    public void DeleteSyncedPool(string key, SyncEntry entry, (string Payload, string Why, string Utc) keep)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        WriteKept(db, entry.Kind, entry.Key, keep.Payload, keep.Why, keep.Utc);
        Execute(db, "DELETE FROM pools WHERE key = $k COLLATE NOCASE", ("$k", key));
        WriteEntry(db, entry);
        transaction.Commit();
    }

    private static long WritePool(SqliteConnection db, PoolRecord p)
    {
        string sheets = JsonSerializer.Serialize(p.Sheets);
        if (p.Id > 0)
        {
            Execute(db, "INSERT INTO pools (id, key, created_utc, sheets_json, choices_json) VALUES ($id, $k, $c, $s, $ch) "
                + "ON CONFLICT(id) DO UPDATE SET key = excluded.key, created_utc = excluded.created_utc, sheets_json = excluded.sheets_json, choices_json = excluded.choices_json",
                ("$id", p.Id), ("$k", p.Key), ("$c", p.CreatedUtc), ("$s", sheets), ("$ch", p.Choices));
            return p.Id;
        }

        Execute(db, "INSERT INTO pools (key, created_utc, sheets_json, choices_json) VALUES ($k, $c, $s, $ch)", ("$k", p.Key), ("$c", p.CreatedUtc), ("$s", sheets), ("$ch", p.Choices));
        return (long)Scalar(db, "SELECT last_insert_rowid()")!;
    }

    private static List<PoolRecord> ReadPools(SqliteConnection db, long? id = null)
    {
        using var command = Command(db, "SELECT id, key, created_utc, sheets_json, choices_json FROM pools WHERE $id IS NULL OR id = $id ORDER BY id", [("$id", id)]);
        using var r = command.ExecuteReader();
        var all = new List<PoolRecord>();
        while (r.Read())
        {
            List<string> sheets;
            try
            {
                sheets = JsonSerializer.Deserialize<List<string>>(r.GetString(3))?.Where(s => s is not null).ToList() ?? [];
            }
            catch (JsonException)
            {
                sheets = [];
            }

            all.Add(new PoolRecord(r.GetInt64(0), r.GetString(1), r.GetString(2), sheets, Text(r, 4)));
        }

        return all;
    }
}
