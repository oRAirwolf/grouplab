using System.Globalization;
using GroupLab.Core.Marking;
using GroupLab.Core.Sync;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Records;

/// <summary>A record as this device last knew it for synchronization: its content's hash, its revision, and the revision last written out.</summary>
public sealed record SyncEntry(string Kind, string Key, string? Hash, VersionVector Vector, string? DeletedUtc, VersionVector Written);

/// <summary>Another device's version of a record changed here too since the two last met, kept for the person to choose.</summary>
public sealed record SyncConflict(long Id, string Kind, string Key, VersionVector Vector, string? Payload, string FromDevice, string FoundUtc);

/// <summary>A record as it was before synchronization changed or took it away, kept so the person can have it back.</summary>
public sealed record SyncKept(long Id, string Kind, string Key, string Payload, string Why, string KeptUtc);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1: what synchronization keeps in the database, schema 6. Every record the export carries has a
/// revision here (<c>sync_records</c>), the change files already read are listed (<c>sync_seen</c>), a record changed on two devices keeps
/// the other device's version for the person (<c>sync_conflicts</c>), and every record a merge replaced or took away is kept as it was
/// (<c>sync_kept</c>). A change from another device is written with its revision in one transaction, so the two never disagree.
/// </summary>
public sealed partial class SessionStore
{
    /// <summary>This device's name in the folder: made once, at random, and kept.</summary>
    public string SyncDevice()
    {
        using var db = Connect();
        if (Scalar(db, "SELECT value FROM meta WHERE key = 'sync_device'") is string device)
        {
            return device;
        }

        device = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        Execute(db, "INSERT INTO meta (key, value) VALUES ('sync_device', $d)", ("$d", device));
        return device;
    }

    public IReadOnlyDictionary<(string Kind, string Key), SyncEntry> SyncLedger()
    {
        using var db = Connect();
        using var command = Command(db, "SELECT kind, key, hash, vector, deleted_utc, written FROM sync_records", []);
        using var r = command.ExecuteReader();
        var all = new Dictionary<(string, string), SyncEntry>();
        while (r.Read())
        {
            var entry = new SyncEntry(r.GetString(0), r.GetString(1), Text(r, 2), VersionVector.Parse(r.GetString(3)), Text(r, 4), VersionVector.Parse(Text(r, 5)));
            all[(entry.Kind, entry.Key)] = entry;
        }

        return all;
    }

    public SyncEntry? SyncEntryOf(string kind, string key)
    {
        using var db = Connect();
        using var command = Command(db, "SELECT kind, key, hash, vector, deleted_utc, written FROM sync_records WHERE kind = $k AND key = $key", [("$k", kind), ("$key", key)]);
        using var r = command.ExecuteReader();
        return r.Read() ? new SyncEntry(r.GetString(0), r.GetString(1), Text(r, 2), VersionVector.Parse(r.GetString(3)), Text(r, 4), VersionVector.Parse(Text(r, 5))) : null;
    }

    public void SetSyncEntry(SyncEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        WriteEntry(db, entry);
    }

    /// <summary>Takes away the revisions of deletions older than <paramref name="beforeUtc"/> that have been written out (the 90 days' markers).</summary>
    public int PruneSyncDeletions(string beforeUtc)
    {
        using var db = Connect();
        using var command = Command(db, "DELETE FROM sync_records WHERE deleted_utc IS NOT NULL AND deleted_utc < $t AND written = vector", [("$t", beforeUtc)]);
        return command.ExecuteNonQuery();
    }

    public bool SyncSeen(string file)
    {
        using var db = Connect();
        return Scalar(db, "SELECT count(*) FROM sync_seen WHERE file = $f", ("$f", file)) is > 0L;
    }

    public void MarkSyncSeen(string file, string utc)
    {
        using var db = Connect();
        Execute(db, "INSERT OR REPLACE INTO sync_seen VALUES ($f, $t)", ("$f", file), ("$t", utc));
    }

    public long AddSyncConflict(string kind, string key, VersionVector vector, string? payload, string fromDevice, string utc)
    {
        ArgumentNullException.ThrowIfNull(vector);
        using var db = Connect();
        Execute(db, "INSERT INTO sync_conflicts (kind, key, vector, payload, from_device, found_utc) VALUES ($k, $key, $v, $p, $d, $t)",
            ("$k", kind), ("$key", key), ("$v", vector.ToString()), ("$p", payload), ("$d", fromDevice), ("$t", utc));
        return (long)Scalar(db, "SELECT last_insert_rowid()")!;
    }

    public IReadOnlyList<SyncConflict> SyncConflicts()
    {
        using var db = Connect();
        using var command = Command(db, "SELECT id, kind, key, vector, payload, from_device, found_utc FROM sync_conflicts ORDER BY id", []);
        using var r = command.ExecuteReader();
        var all = new List<SyncConflict>();
        while (r.Read())
        {
            all.Add(new SyncConflict(r.GetInt64(0), r.GetString(1), r.GetString(2), VersionVector.Parse(r.GetString(3)), Text(r, 4), r.GetString(5), r.GetString(6)));
        }

        return all;
    }

    public void RemoveSyncConflict(long id)
    {
        using var db = Connect();
        Execute(db, "DELETE FROM sync_conflicts WHERE id = $id", ("$id", id));
    }

    /// <summary>Keeps a record as it was, before synchronization changes it, outside any transaction of its own.</summary>
    public long Keep(string kind, string key, string payload, string why, string utc)
    {
        using var db = Connect();
        return WriteKept(db, kind, key, payload, why, utc);
    }

    public IReadOnlyList<SyncKept> Kept()
    {
        using var db = Connect();
        using var command = Command(db, "SELECT id, kind, key, payload, why, kept_utc FROM sync_kept ORDER BY id", []);
        using var r = command.ExecuteReader();
        var all = new List<SyncKept>();
        while (r.Read())
        {
            all.Add(new SyncKept(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5)));
        }

        return all;
    }

    /// <summary>
    /// A session another device changed, written here with its chronograph strings and its revision in one transaction, after the session as it
    /// was is kept. A session new here gets a new id; one already here keeps its id and its original photograph's path on this device.
    /// </summary>
    public long ApplySyncedSession(long? localId, SessionRecord incoming, IReadOnlyList<ChronographString> strings, IReadOnlyList<ShotVelocity> velocities,
        SyncEntry entry, (string Payload, string Why, string Utc)? keep)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(velocities);
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        if (keep is { } k)
        {
            WriteKept(db, entry.Kind, entry.Key, k.Payload, k.Why, k.Utc);
        }

        string? path = localId is { } here ? Scalar(db, "SELECT image_path FROM sessions WHERE id = $id", ("$id", here)) as string : null;
        long id = WriteSession(db, incoming with { Id = localId ?? 0, ImagePath = path });
        Execute(db, "DELETE FROM chronograph_strings WHERE session_id = $s", ("$s", id));
        var ids = new Dictionary<long, long>();
        foreach (var s in strings)
        {
            Execute(db, "INSERT INTO chronograph_strings (session_id, source, recorded_utc) VALUES ($s, $src, $at)", ("$s", id), ("$src", s.Source), ("$at", s.RecordedUtc));
            long stringId = (long)Scalar(db, "SELECT last_insert_rowid()")!;
            ids[s.Id] = stringId;
            for (int i = 0; i < s.VelocitiesFps.Count; i++)
            {
                Execute(db, "INSERT INTO chronograph_shots VALUES ($id, $o, $v)", ("$id", stringId), ("$o", i + 1), ("$v", s.VelocitiesFps[i]));
            }
        }

        foreach (var v in velocities)
        {
            if (ids.TryGetValue(v.StringId, out long stringId))
            {
                Execute(db, "INSERT OR REPLACE INTO shot_velocities VALUES ($s, $shot, $str, $o)", ("$s", id), ("$shot", v.ShotId), ("$str", stringId), ("$o", v.Ordinal));
            }
        }

        WriteEntry(db, entry);
        transaction.Commit();
        return id;
    }

    /// <summary>A session another device took away, deleted here with its revision in one transaction, after it is kept as it was.</summary>
    public void DeleteSyncedSession(long localId, SyncEntry entry, (string Payload, string Why, string Utc) keep)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        WriteKept(db, entry.Kind, entry.Key, keep.Payload, keep.Why, keep.Utc);
        Execute(db, "DELETE FROM sessions WHERE id = $id", ("$id", localId));
        WriteEntry(db, entry);
        transaction.Commit();
    }

    /// <summary>The rifles, barrels and loads with one changed by another device, and its revision, in one transaction.</summary>
    public void ApplySyncedBook(RecordBook book, SyncEntry entry, (string Payload, string Why, string Utc)? keep)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(entry);
        using var db = Connect();
        using var transaction = db.BeginTransaction();
        if (keep is { } k)
        {
            WriteKept(db, entry.Kind, entry.Key, k.Payload, k.Why, k.Utc);
        }

        WriteBook(db, book);
        WriteEntry(db, entry);
        transaction.Commit();
    }

    private static void WriteEntry(SqliteConnection db, SyncEntry entry) =>
        Execute(db, "INSERT OR REPLACE INTO sync_records VALUES ($k, $key, $h, $v, $d, $w)", ("$k", entry.Kind), ("$key", entry.Key), ("$h", entry.Hash),
            ("$v", entry.Vector.ToString()), ("$d", entry.DeletedUtc), ("$w", entry.Written.ToString()));

    private static long WriteKept(SqliteConnection db, string kind, string key, string payload, string why, string utc)
    {
        Execute(db, "INSERT INTO sync_kept (kind, key, payload, why, kept_utc) VALUES ($k, $key, $p, $w, $t)", ("$k", kind), ("$key", key), ("$p", payload), ("$w", why), ("$t", utc));
        return Convert.ToInt64(Scalar(db, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }
}
