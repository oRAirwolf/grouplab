using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Core.Sync;

/// <summary>What a run is told to do beyond the geometry and the proof pictures, which always travel.</summary>
/// <param name="Originals">Also copy the original photographs into the folder: only when the person has asked for it (tier 3).</param>
/// <param name="DeletionDays">How long a deletion's marker is kept here once it has been written out.</param>
public sealed record SyncOptions(bool Originals = false, int DeletionDays = 90);

/// <summary>What a run did, in numbers and in a few plain lines.</summary>
public sealed record SyncReport(bool Reached, int FilesRead, int FilesWaiting, int Applied, int ToChoose, int Written, int OriginalsCopied, IReadOnlyList<string> Said);

/// <summary>What the person chose for a record changed on two devices.</summary>
public enum SyncChoice
{
    /// <summary>Keep this device's version; the other device's is kept as it was.</summary>
    KeepThisOne,

    /// <summary>Take the other device's version; this device's is kept as it was.</summary>
    KeepTheOther,

    /// <summary>Keep both, the other device's as a session of its own (sessions only).</summary>
    KeepBoth,

    /// <summary>Close a note that asks nothing, such as a deletion on one device of a record changed on another, already settled by keeping the change.</summary>
    Understood,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3, option A of docs/notes/sync-options.md, chosen by Alan on 2026-10-20: GroupLab writes small
/// change files into a folder the person's own cloud client already syncs and reads the other devices' change files from it. Nothing a
/// person can reach starts it yet; <c>grouplab sync</c> does, for testing.
/// <list type="bullet">
/// <item>The folder holds <c>GroupLab sync/devices/&lt;device&gt;.json</c>, each device's name and when it last wrote, and
/// <c>GroupLab sync/changes/&lt;device&gt;/&lt;time&gt;-&lt;random&gt;.json</c>, written only by that device, so two devices never write the same file
/// and no cloud client makes a conflicted copy. A file is written beside its name and renamed into place.</item>
/// <item>A change file holds whole records: a session with its marks, its proof picture and its chronograph strings, a rifle, barrel or load,
/// a designed sheet or a portable setting, each with its identity (a session's is <see cref="DataExport.Identity"/>) and its revision
/// (<see cref="VersionVector"/>), or a deletion's marker. It ends with <c>"end": true</c>; a file without it, as a cloud client delivers one
/// in the middle of copying it, is left to be read on a later run.</item>
/// <item>The merge: a record changed on one device wins; the same record changed on two devices since they last met is kept both ways and
/// listed for the person to choose, never decided silently. A deletion against a change keeps the change, and says so. Before a merge
/// changes or takes away a record here, the record as it was is kept (<see cref="SessionStore.Kept"/>), so nothing is lost.</item>
/// <item>Tiers: geometry and the proof picture always; the original photographs only when the person has asked for them.</item>
/// </list>
/// </summary>
public static class SyncEngine
{
    public const string Root = "GroupLab sync";

    public const string Format = "grouplab-sync";

    public const int Version = 1;

    /// <summary>How often a run is due while GroupLab is open, besides the one when it starts.</summary>
    public static TimeSpan Every { get; } = TimeSpan.FromMinutes(5);

    /// <summary>The most sessions one change file carries, so a first run with a year of targets writes files a cloud client moves easily.</summary>
    public const int SessionsAFile = 25;

    public const string SessionKind = "session";

    public const string RifleKind = "rifle";

    public const string BarrelKind = "barrel";

    public const string LoadKind = "load";

    public const string SheetKind = "sheet";

    public const string SettingKind = "setting";

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>One record as it is here now: what a change file would carry for it, and its hash.</summary>
    private sealed record Here(JsonObject Payload, string Hash, long? SessionId);

    public static SyncReport Run(SessionStore store, ISyncLocal local, ISyncFolder folder, string deviceName, DateTime nowUtc, SyncOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(folder);
        options ??= new SyncOptions();
        if (!folder.Available)
        {
            return new SyncReport(false, 0, 0, 0, store.SyncConflicts().Count, 0, 0,
                ["The sync folder is not there now (unplugged, signed out of or moved), so nothing was read or written. Everything here is as it was."]);
        }

        string me = store.SyncDevice();
        string now = Utc(nowUtc);
        int read = 0, waiting = 0, applied = 0, written = 0, originals = 0;
        var said = new List<string>();
        try
        {
            // 1. This device's own changes since the last run, each given a new revision.
            var current = Current(store, local);
            Scan(store, current, me, now);

            // 2. Every other device's change files not yet read, oldest first.
            foreach (string device in folder.Folders(Root + "/changes").Where(d => d != me))
            {
                foreach (string name in folder.Files($"{Root}/changes/{device}").Where(IsChangeFile))
                {
                    string seen = device + "/" + name;
                    if (store.SyncSeen(seen))
                    {
                        continue;
                    }

                    if (ReadChangeFile(folder.Read($"{Root}/changes/{device}/{name}")) is not { } records)
                    {
                        waiting++;
                        continue;
                    }

                    foreach (var record in records)
                    {
                        applied += Merge(store, local, current, record, device, me, now) ? 1 : 0;
                    }

                    store.MarkSyncSeen(seen, now);
                    read++;
                }
            }

            // 3. This device's changes, written out once each.
            written = WriteChanges(store, current, folder, me, nowUtc);

            // 4. The original photographs, only when asked for.
            if (options.Originals)
            {
                originals = CopyOriginals(store, folder);
            }

            store.PruneSyncDeletions(Utc(nowUtc.AddDays(-options.DeletionDays)));
            if (written > 0 || !folder.Exists($"{Root}/devices/{me}.json"))
            {
                var device = new JsonObject { ["format"] = Format, ["version"] = Version, ["device"] = me, ["name"] = deviceName, ["lastWrittenUtc"] = now };
                folder.Write($"{Root}/devices/{me}.json", Encoding.UTF8.GetBytes(device.ToJsonString()));
            }
        }
        catch (IOException e)
        {
            said.Add("The sync folder stopped answering part way (" + e.Message + "); what was read so far is kept, and the rest is read next time.");
        }
        catch (UnauthorizedAccessException)
        {
            said.Add("GroupLab may no longer write to the sync folder; what was read so far is kept. Choose the folder again.");
        }

        int toChoose = store.SyncConflicts().Count;
        said.Insert(0, $"{applied} change{(applied == 1 ? "" : "s")} from other devices in {read} file{(read == 1 ? "" : "s")}, {written} written from this one"
            + (waiting > 0 ? $", {waiting} file{(waiting == 1 ? "" : "s")} still arriving" : "")
            + (toChoose > 0 ? $", {toChoose} to choose" : "") + ".");
        return new SyncReport(true, read, waiting, applied, toChoose, written, originals, said);
    }

    /// <summary>The person's choice for a record changed on two devices: whichever is kept, the other is kept as it was too.</summary>
    public static void Resolve(SessionStore store, ISyncLocal local, long conflictId, SyncChoice choice, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(local);
        var conflict = store.SyncConflicts().FirstOrDefault(c => c.Id == conflictId)
            ?? throw new ArgumentException("No record waiting to be chosen has that number.", nameof(conflictId));
        string me = store.SyncDevice();
        string now = Utc(nowUtc);
        var ledger = store.SyncLedger();
        var entry = ledger.TryGetValue((conflict.Kind, conflict.Key), out var e) ? e : new SyncEntry(conflict.Kind, conflict.Key, null, VersionVector.Empty, null, VersionVector.Empty);
        var merged = entry.Vector.Merge(conflict.Vector).Bump(me);
        var current = Current(store, local);
        current.TryGetValue((conflict.Kind, conflict.Key), out var here);
        switch (choice)
        {
            case SyncChoice.Understood:
                break;
            case SyncChoice.KeepThisOne:
                if (conflict.Payload is { } other)
                {
                    store.Keep(conflict.Kind, conflict.Key, other, $"the version from device {conflict.FromDevice}, not chosen", now);
                }

                store.SetSyncEntry(entry with { Vector = merged });
                break;
            case SyncChoice.KeepTheOther when conflict.Payload is { } theirs:
                var record = new ChangeRecord(conflict.Kind, conflict.Key, conflict.Vector, null, (JsonObject)JsonNode.Parse(theirs)!);
                Apply(store, local, current, record, here, entry with { Vector = merged, DeletedUtc = null }, here is null ? null : (here.Payload.ToJsonString(), "this device's version, replaced as chosen", now));
                break;
            case SyncChoice.KeepBoth when conflict.Kind == SessionKind && conflict.Payload is { } both:
                // The other device's session as one of its own: saved as new, made now, so it has an identity of its own and travels as new.
                var (session, strings, velocities) = SessionFrom((JsonObject)JsonNode.Parse(both)!);
                long id = store.Save(session with { Id = 0, ImagePath = null, CreatedUtc = now });
                var ids = new Dictionary<long, long>();
                foreach (var s in strings)
                {
                    ids[s.Id] = store.AddChronographString(id, s.Source, s.RecordedUtc, s.VelocitiesFps);
                }

                foreach (var v in velocities.Where(v => ids.ContainsKey(v.StringId)))
                {
                    store.MapShot(v with { SessionId = id, StringId = ids[v.StringId] });
                }

                store.SetSyncEntry(entry with { Vector = merged });
                break;
            default:
                throw new ArgumentException($"{choice} does not apply to this record.", nameof(choice));
        }

        store.RemoveSyncConflict(conflict.Id);
    }

    // ---- What is here.

    private static Dictionary<(string Kind, string Key), Here> Current(SessionStore store, ISyncLocal local)
    {
        var all = Outside(store, local);
        foreach (var summary in store.List().OrderBy(s => s.Id))
        {
            if (store.Get(summary.Id) is not { } s)
            {
                continue;
            }

            var payload = SessionPayload(s, store.ChronographStrings(s.Id), store.ShotVelocities(s.Id));
            string key = DataExport.Identity(s);
            for (int n = 2; all.ContainsKey((SessionKind, key)); n++)
            {
                key = DataExport.Identity(s) + "#" + n.ToString(CultureInfo.InvariantCulture);
            }

            all[(SessionKind, key)] = new Here(payload, Hash(payload), s.Id);
        }

        return all;
    }

    /// <summary>Everything but the sessions: the rifles, barrels and loads, the designed sheets and the portable settings, which are few and small.</summary>
    private static Dictionary<(string Kind, string Key), Here> Outside(SessionStore store, ISyncLocal local)
    {
        var all = new Dictionary<(string, string), Here>();
        var book = store.LoadBook();
        foreach (var rifle in book.Rifles)
        {
            Add(all, RifleKind, rifle.Name, JsonSerializer.SerializeToNode(rifle, Options)!.AsObject());
        }

        foreach (var barrel in book.Barrels)
        {
            Add(all, BarrelKind, barrel.Name, JsonSerializer.SerializeToNode(barrel, Options)!.AsObject());
        }

        foreach (var load in book.Loads)
        {
            Add(all, LoadKind, load.Name, JsonSerializer.SerializeToNode(load, Options)!.AsObject());
        }

        foreach (var sheet in local.Sheets())
        {
            Add(all, SheetKind, sheet.File, JsonSerializer.SerializeToNode(sheet, Options)!.AsObject());
        }

        foreach (var (key, value) in local.Settings())
        {
            if (value is not null && DataExport.PortableSettings.Contains(key))
            {
                Add(all, SettingKind, key, new JsonObject { ["key"] = key, ["value"] = value.DeepClone() });
            }
        }

        return all;
    }

    private static void Add(Dictionary<(string, string), Here> all, string kind, string name, JsonObject payload) =>
        all[(kind, name.ToLowerInvariant())] = new Here(payload, Hash(payload), null);

    /// <summary>The one record a change touched, read again as it now is here.</summary>
    private static Here? Refresh(SessionStore store, ISyncLocal local, Dictionary<(string Kind, string Key), Here> current, string kind, string key, long? sessionId)
    {
        current.Remove((kind, key));
        Here? now = null;
        if (kind == SessionKind)
        {
            if (sessionId is { } id && store.Get(id) is { } s)
            {
                var payload = SessionPayload(s, store.ChronographStrings(id), store.ShotVelocities(id));
                now = new Here(payload, Hash(payload), id);
            }
        }
        else
        {
            now = Outside(store, local).GetValueOrDefault((kind, key));
        }

        if (now is not null)
        {
            current[(kind, key)] = now;
        }

        return now;
    }

    internal static JsonObject SessionPayload(SessionRecord s, IReadOnlyList<ChronographString> strings, IReadOnlyList<ShotVelocity> velocities)
    {
        // As the export carries it: no id, no path on this device, no folders in the marking; the strings by their place, since their ids
        // are this database's own.
        var place = strings.Select((c, i) => (c.Id, i)).ToDictionary(p => p.Id, p => (long)p.i);
        return new JsonObject
        {
            ["session"] = JsonSerializer.SerializeToNode(s with { Id = 0, ImagePath = null, MarkingJson = MarkingFile.WithoutFolders(s.MarkingJson) }, Options),
            ["strings"] = JsonSerializer.SerializeToNode(strings.Select((c, i) => c with { Id = i, SessionId = 0 }).ToList(), Options),
            ["velocities"] = JsonSerializer.SerializeToNode(velocities.Where(v => place.ContainsKey(v.StringId))
                .Select(v => v with { SessionId = 0, StringId = place[v.StringId] }).OrderBy(v => v.ShotId).ThenBy(v => v.StringId).ToList(), Options),
        };
    }

    private static (SessionRecord Session, List<ChronographString> Strings, List<ShotVelocity> Velocities) SessionFrom(JsonObject payload) =>
        (payload["session"].Deserialize<SessionRecord>(Options) ?? throw new InvalidDataException("a session without its record"),
         payload["strings"]?.Deserialize<List<ChronographString>>(Options) ?? [],
         payload["velocities"]?.Deserialize<List<ShotVelocity>>(Options) ?? []);

    private static string Hash(JsonObject payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToJsonString()))).ToLowerInvariant();

    /// <summary>Each record new or changed here since the last run gets one more change on this device; each gone gets a deletion's marker.</summary>
    private static void Scan(SessionStore store, Dictionary<(string Kind, string Key), Here> current, string me, string now)
    {
        var ledger = store.SyncLedger();
        foreach (var ((kind, key), here) in current)
        {
            if (!ledger.TryGetValue((kind, key), out var entry))
            {
                store.SetSyncEntry(new SyncEntry(kind, key, here.Hash, VersionVector.Empty.Bump(me), null, VersionVector.Empty));
            }
            else if (entry.DeletedUtc is not null || entry.Hash != here.Hash)
            {
                store.SetSyncEntry(entry with { Hash = here.Hash, Vector = entry.Vector.Bump(me), DeletedUtc = null });
            }
        }

        foreach (var entry in ledger.Values.Where(e => e.DeletedUtc is null && !current.ContainsKey((e.Kind, e.Key))))
        {
            store.SetSyncEntry(entry with { Hash = null, Vector = entry.Vector.Bump(me), DeletedUtc = now });
        }
    }

    // ---- The change files.

    /// <summary>One record in a change file: its identity, its revision, and the record itself or when it was taken away.</summary>
    internal sealed record ChangeRecord(string Kind, string Key, VersionVector Vector, string? DeletedUtc, JsonObject? Data);

    private static bool IsChangeFile(string name) => name.EndsWith(".json", StringComparison.Ordinal) && !name.StartsWith('.');

    /// <summary>The records of a change file, or null where it is not whole yet (or not one this GroupLab can read), to be tried again later.</summary>
    internal static List<ChangeRecord>? ReadChangeFile(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            if (JsonNode.Parse(bytes) is not JsonObject file || (string?)file["format"] != Format || file["end"]?.GetValueKind() != JsonValueKind.True
                || (int?)file["version"] is not { } version || version < 1 || version > Version || file["records"] is not JsonArray records)
            {
                return null;
            }

            var all = new List<ChangeRecord>();
            foreach (var node in records.OfType<JsonObject>())
            {
                if ((string?)node["kind"] is not { } kind || (string?)node["key"] is not { } key)
                {
                    continue;
                }

                all.Add(new ChangeRecord(kind, key, VersionVector.Parse(node["vector"]), (string?)node["deletedUtc"], node["data"] as JsonObject is { } data ? (JsonObject)data.DeepClone() : null));
            }

            return all;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    private static int WriteChanges(SessionStore store, Dictionary<(string Kind, string Key), Here> current, ISyncFolder folder, string me, DateTime nowUtc)
    {
        var due = store.SyncLedger().Values.Where(e => !e.Written.Equals(e.Vector)).OrderBy(e => e.Kind, StringComparer.Ordinal).ThenBy(e => e.Key, StringComparer.Ordinal).ToList();
        if (due.Count == 0)
        {
            return 0;
        }

        var batch = new List<(SyncEntry Entry, JsonObject Node)>();
        int sessions = 0, files = 0;
        void Flush()
        {
            if (batch.Count == 0)
            {
                return;
            }

            var records = new JsonArray();
            foreach (var (_, node) in batch)
            {
                records.Add(node);
            }

            var file = new JsonObject { ["format"] = Format, ["version"] = Version, ["device"] = me, ["writtenUtc"] = Utc(nowUtc), ["records"] = records, ["end"] = true };
            string name = string.Create(CultureInfo.InvariantCulture, $"{nowUtc:yyyyMMdd'T'HHmmssfff'Z'}-{files:000}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant()}.json");
            folder.Write($"{Root}/changes/{me}/{name}", Encoding.UTF8.GetBytes(file.ToJsonString()));

            // Only once the file is in place is its records' revision said to be written.
            foreach (var (entry, _) in batch)
            {
                store.SetSyncEntry(entry with { Written = entry.Vector });
            }

            files++;
            batch.Clear();
            sessions = 0;
        }

        int count = 0;
        foreach (var entry in due)
        {
            var node = new JsonObject { ["kind"] = entry.Kind, ["key"] = entry.Key, ["vector"] = entry.Vector.ToJson() };
            if (entry.DeletedUtc is not null)
            {
                node["deletedUtc"] = entry.DeletedUtc;
            }
            else if (current.TryGetValue((entry.Kind, entry.Key), out var here))
            {
                node["data"] = here.Payload.DeepClone();
                sessions += entry.Kind == SessionKind ? 1 : 0;
            }
            else
            {
                continue;
            }

            batch.Add((entry, node));
            count++;
            if (sessions >= SessionsAFile)
            {
                Flush();
            }
        }

        Flush();
        return count;
    }

    // ---- The merge.

    /// <summary>One record from another device merged here; true where it changed something here.</summary>
    private static bool Merge(SessionStore store, ISyncLocal local, Dictionary<(string Kind, string Key), Here> current, ChangeRecord record, string from, string me, string now)
    {
        var key = (record.Kind, record.Key);
        bool deleted = record.DeletedUtc is not null || record.Data is null;
        if (store.SyncEntryOf(record.Kind, record.Key) is not { } entry)
        {
            var fresh = new SyncEntry(record.Kind, record.Key, null, record.Vector, deleted ? record.DeletedUtc ?? now : null, record.Vector);
            if (deleted)
            {
                // A record never here, already taken away: its marker is remembered, so an older copy arriving later is not brought back.
                store.SetSyncEntry(fresh);
                return false;
            }

            Apply(store, local, current, record, null, fresh, null);
            return true;
        }

        var here = current.GetValueOrDefault(key);
        switch (record.Vector.Compare(entry.Vector))
        {
            case Order.Same or Order.Older:
                return false;

            case Order.Newer:
                var keep = here is null ? ((string, string, string)?)null : (here.Payload.ToJsonString(), $"replaced by a newer version from device {from}", now);
                if (deleted)
                {
                    if (here is not null)
                    {
                        Remove(store, local, current, record.Kind, record.Key, here, entry with { Hash = null, Vector = record.Vector, DeletedUtc = record.DeletedUtc ?? now, Written = record.Vector },
                            (here.Payload.ToJsonString(), $"taken away on device {from}", now));
                    }
                    else
                    {
                        store.SetSyncEntry(entry with { Hash = null, Vector = record.Vector, DeletedUtc = record.DeletedUtc ?? now, Written = record.Vector });
                    }
                }
                else
                {
                    Apply(store, local, current, record, here, entry with { Vector = record.Vector, DeletedUtc = null, Written = record.Vector }, keep);
                }

                // A choice waiting for this record is settled by a version made from it.
                foreach (var waiting in store.SyncConflicts().Where(c => c.Kind == record.Kind && c.Key == record.Key && c.Vector.Compare(record.Vector) is Order.Older or Order.Same))
                {
                    store.RemoveSyncConflict(waiting.Id);
                }

                if (!deleted && entry.DeletedUtc is not null)
                {
                    // Taken away here, and brought back by a change made on a device that had seen it taken away: said here too.
                    store.AddSyncConflict(record.Kind, record.Key, record.Vector, null, from, now);
                }

                return true;

            default:
                bool deletedHere = entry.DeletedUtc is not null;
                var merged = entry.Vector.Merge(record.Vector);
                if (deleted && deletedHere)
                {
                    store.SetSyncEntry(entry with { Vector = merged });
                    return false;
                }

                if (deleted)
                {
                    // Taken away there, changed here: the change is kept, travels back as the newer version, and is listed.
                    store.SetSyncEntry(entry with { Vector = merged.Bump(me) });
                    store.AddSyncConflict(record.Kind, record.Key, record.Vector, null, from, now);
                    return false;
                }

                if (deletedHere)
                {
                    // Taken away here, changed there: the change is brought back here, and listed.
                    Apply(store, local, current, record, null, entry with { Vector = merged, DeletedUtc = null }, null);
                    store.AddSyncConflict(record.Kind, record.Key, record.Vector, null, from, now);
                    return true;
                }

                // Changed on both since they last met: kept both ways, this device's in place and the other's waiting for the person. A
                // later version from the same device replaces its earlier one in the list.
                foreach (var older in store.SyncConflicts().Where(c => c.Kind == record.Kind && c.Key == record.Key && c.FromDevice == from && c.Vector.Compare(record.Vector) is Order.Older))
                {
                    store.RemoveSyncConflict(older.Id);
                }

                store.AddSyncConflict(record.Kind, record.Key, record.Vector, record.Data!.ToJsonString(), from, now);
                return false;
        }
    }

    /// <summary>Writes another device's version of a record here, keeping the version it replaces first.</summary>
    private static void Apply(SessionStore store, ISyncLocal local, Dictionary<(string Kind, string Key), Here> current, ChangeRecord record, Here? here, SyncEntry entry,
        (string Payload, string Why, string Utc)? keep)
    {
        var data = record.Data!;
        entry = entry with { Hash = Hash(data) };
        long? sessionId = null;
        switch (record.Kind)
        {
            case SessionKind:
                var (session, strings, velocities) = SessionFrom(data);
                sessionId = store.ApplySyncedSession(here?.SessionId, session, strings, velocities, entry, keep);
                break;
            case RifleKind or BarrelKind or LoadKind:
                var book = Without(store.LoadBook(), record.Kind, record.Key);
                book = record.Kind switch
                {
                    RifleKind => book.With(data.Deserialize<Rifle>(Options)!),
                    BarrelKind => book.With(data.Deserialize<Barrel>(Options)!),
                    _ => book.With(data.Deserialize<Load>(Options)!),
                };
                store.ApplySyncedBook(book, entry, keep);
                break;
            case SheetKind:
                KeepFirst(store, entry, keep);
                local.SaveSheet(data.Deserialize<ExportedSheet>(Options)!);
                store.SetSyncEntry(entry);
                break;
            case SettingKind:
                KeepFirst(store, entry, keep);
                local.SaveSetting((string?)data["key"] ?? record.Key, data["value"]?.DeepClone());
                store.SetSyncEntry(entry);
                break;
            default:
                // A kind a newer GroupLab writes and this one does not know: remembered, so it is not asked about again, and left alone.
                store.SetSyncEntry(entry);
                return;
        }

        // What the change became here is what this device now has: its hash as it reads back, with no new revision, so the next run does
        // not take it for a change made here.
        if (Refresh(store, local, current, record.Kind, record.Key, sessionId) is { } now && now.Hash != entry.Hash)
        {
            store.SetSyncEntry(entry with { Hash = now.Hash });
        }
    }

    private static void Remove(SessionStore store, ISyncLocal local, Dictionary<(string Kind, string Key), Here> current, string kind, string key, Here here, SyncEntry entry,
        (string Payload, string Why, string Utc) keep)
    {
        current.Remove((kind, key));
        switch (kind)
        {
            case SessionKind when here.SessionId is { } id:
                store.DeleteSyncedSession(id, entry, keep);
                break;
            case RifleKind or BarrelKind or LoadKind:
                store.ApplySyncedBook(Without(store.LoadBook(), kind, key), entry, keep);
                break;
            case SheetKind:
                KeepFirst(store, entry, keep);
                local.DeleteSheet((string?)here.Payload["file"] ?? key);
                store.SetSyncEntry(entry);
                break;
            case SettingKind:
                KeepFirst(store, entry, keep);
                local.SaveSetting(key, null);
                store.SetSyncEntry(entry);
                break;
            default:
                store.SetSyncEntry(entry);
                break;
        }
    }

    private static void KeepFirst(SessionStore store, SyncEntry entry, (string Payload, string Why, string Utc)? keep)
    {
        if (keep is { } k)
        {
            store.Keep(entry.Kind, entry.Key, k.Payload, k.Why, k.Utc);
        }
    }

    private static RecordBook Without(RecordBook book, string kind, string key) => kind switch
    {
        RifleKind => book with { Rifles = [.. book.Rifles.Where(r => r.Name.ToLowerInvariant() != key)] },
        BarrelKind => book with { Barrels = [.. book.Barrels.Where(b => b.Name.ToLowerInvariant() != key)] },
        _ => book with { Loads = [.. book.Loads.Where(l => l.Name.ToLowerInvariant() != key)] },
    };

    // ---- The original photographs.

    private static int CopyOriginals(SessionStore store, ISyncFolder folder)
    {
        int copied = 0;
        foreach (var summary in store.List())
        {
            if (store.Get(summary.Id) is not { ImagePath: { } path, ImageSha256: { } sha } || !File.Exists(path))
            {
                continue;
            }

            string name = $"{Root}/originals/{sha}{System.IO.Path.GetExtension(path).ToLowerInvariant()}";
            if (!folder.Exists(name))
            {
                folder.Write(name, File.ReadAllBytes(path));
                copied++;
            }
        }

        return copied;
    }

    private static string Utc(DateTime t) => t.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
