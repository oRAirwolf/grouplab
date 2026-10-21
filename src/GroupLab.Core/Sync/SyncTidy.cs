using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Records;

namespace GroupLab.Core.Sync;

/// <summary>What one tidying did: old change files moved to the sync folder's trash and their size, and tidied files removed from the trash after their days and theirs.</summary>
public sealed record SyncTidied(int Moved, long MovedBytes, int Removed, long FreedBytes)
{
    public static SyncTidied None { get; } = new(0, 0, 0, 0);

    public bool Any => Moved > 0 || Removed > 0;
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 464 section 1: tidying old change files. A session changed again is written again whole, so without it a
/// much-edited session costs a few times its size in the person's folder. Each device tidies only its own change files, as it is the only one
/// that writes them, and a file of its own is moved only when all of these hold:
/// <list type="bullet">
/// <item>Every other device that has written to the folder (a change folder, a device file or a read position of its own) has a read position
/// that can be read whole, <c>GroupLab sync/read/&lt;device&gt;.json</c>, written by that device, and it lists this file among the files it has
/// read. A device whose position is missing or cannot be read has not been seen, and then nothing is moved at all. Nothing is guessed: not
/// from the time, not from the file's name.</item>
/// <item>Every record in it has a newer version in a later change file of this device that stays, so a device that joins later, or one that
/// has not read this file yet, still ends with the same records. A record written once and never changed keeps its file for ever.</item>
/// <item>No record in it is waiting for the person to choose between two versions (entry 463's side by side) on any device: each device's
/// read position lists the records waiting there, and this device's own are taken from its database.</item>
/// </list>
/// A moved file goes whole to <c>GroupLab sync/trash/&lt;device&gt;/</c>, named after the time it was moved, and is taken away only once
/// <see cref="SyncEngine.TrashDays"/> days have passed since then (Alan's rule on deleting). Its copy is written beside its name and renamed
/// into place, read back and compared before the original is taken away, so a folder that goes away part way leaves each file either moved or
/// where it was. The trash is outside <c>changes</c>, so no device ever reads it as change files.
/// </summary>
internal static class SyncTidy
{
    public const string PositionFormat = "grouplab-sync-read";

    /// <summary>Between the time a file was moved and its original name in the trash.</summary>
    private const char Mark = '~';

    /// <summary>One other device's read position: the change files it has read, by the device that wrote them, and the records waiting there for the person.</summary>
    private sealed record Position(IReadOnlyDictionary<string, HashSet<string>> Read, IReadOnlyList<(string Kind, string Key)> Waiting);

    /// <summary>
    /// Writes this device's read position: for each other device's change folder, the files in it this device has read, and the records
    /// waiting here for the person to choose. Written only when it differs from the one in the folder.
    /// </summary>
    public static void WritePosition(SessionStore store, ISyncFolder folder, string me, IReadOnlyDictionary<string, List<string>> read)
    {
        var devices = new JsonObject();
        foreach (var (device, names) in read.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var list = new JsonArray();
            foreach (string name in names.Distinct().Order(StringComparer.Ordinal))
            {
                list.Add(name);
            }

            devices[device] = list;
        }

        var waiting = new JsonArray();
        foreach (var (kind, key) in store.SyncConflicts().Select(c => (c.Kind, c.Key)).Distinct().OrderBy(w => w.Kind, StringComparer.Ordinal).ThenBy(w => w.Key, StringComparer.Ordinal))
        {
            waiting.Add(new JsonObject { ["kind"] = kind, ["key"] = key });
        }

        var position = new JsonObject
        {
            ["format"] = PositionFormat, ["version"] = SyncEngine.Version, ["device"] = me, ["read"] = devices, ["waiting"] = waiting, ["end"] = true,
        };
        byte[] bytes = Encoding.UTF8.GetBytes(position.ToJsonString());
        string path = $"{SyncEngine.Root}/read/{me}.json";
        if (folder.Read(path) is not { } there || !there.AsSpan().SequenceEqual(bytes))
        {
            folder.Write(path, bytes);
        }
    }

    /// <summary>
    /// Tidies, at most once in <see cref="SyncOptions.TidyHours"/>: empties what has been in the trash its days, then moves the old change
    /// files that may go. <paramref name="done"/> is counted as each file is moved or removed, so a run the folder stops answering part way
    /// still says what was done; the time is kept only once a tidying finished.
    /// </summary>
    public static void Run(SessionStore store, ISyncFolder folder, string me, DateTime nowUtc, SyncOptions options, Counted done)
    {
        if (store.SyncTidiedUtc() is { } last && DateTime.TryParse(last, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            && nowUtc - at < TimeSpan.FromHours(options.TidyHours))
        {
            return;
        }

        Empty(folder, me, nowUtc, options, done);
        Move(store, folder, me, nowUtc, done);
        store.MarkSyncTidied(SyncEngine.Utc(nowUtc));
    }

    /// <summary>The counts as a tidying goes.</summary>
    internal sealed class Counted
    {
        public int Moved { get; set; }

        public long MovedBytes { get; set; }

        public int Removed { get; set; }

        public long FreedBytes { get; set; }

        public SyncTidied Result => new(Moved, MovedBytes, Removed, FreedBytes);
    }

    private static void Move(SessionStore store, ISyncFolder folder, string me, DateTime nowUtc, Counted done)
    {
        // Every other device that has written to the folder, and its read position; one not seen stops it all.
        var positions = new Dictionary<string, Position>();
        foreach (string device in Devices(folder, me))
        {
            if (ReadPosition(folder.Read($"{SyncEngine.Root}/read/{device}.json"), device) is not { } position)
            {
                return;
            }

            positions[device] = position;
        }

        string mine = $"{SyncEngine.Root}/changes/{me}";
        var files = folder.Files(mine).Where(SyncEngine.IsChangeFile).Order(StringComparer.Ordinal).ToList();
        var readByAll = files.Take(files.Count - 1)
            .Where(name => positions.Values.All(p => p.Read.TryGetValue(me, out var read) && read.Contains(name))).ToHashSet(StringComparer.Ordinal);
        if (readByAll.Count == 0)
        {
            return;
        }

        var waiting = positions.Values.SelectMany(p => p.Waiting).Concat(store.SyncConflicts().Select(c => (c.Kind, c.Key))).ToHashSet();

        // From the newest file back: a file may go when each of its records has a newer version in a later file, which stays.
        var later = new Dictionary<(string Kind, string Key), List<VersionVector>>();
        var go = new List<string>();
        for (int i = files.Count - 1; i >= 0; i--)
        {
            if (SyncEngine.ReadChangeFile(folder.Read($"{mine}/{files[i]}")) is not { } records)
            {
                // One that cannot be read here now is left, and is not taken to carry anything newer either.
                continue;
            }

            if (readByAll.Contains(files[i]) && records.All(r => !waiting.Contains((r.Kind, r.Key))
                && later.TryGetValue((r.Kind, r.Key), out var newer) && newer.Any(v => v.Compare(r.Vector) is Order.Newer or Order.Same)))
            {
                go.Add(files[i]);
            }

            foreach (var r in records)
            {
                if (!later.TryGetValue((r.Kind, r.Key), out var list))
                {
                    later[(r.Kind, r.Key)] = list = [];
                }

                list.Add(r.Vector);
            }
        }

        string stamp = string.Create(CultureInfo.InvariantCulture, $"{nowUtc:yyyyMMdd'T'HHmmssfff'Z'}");
        foreach (string name in go.Order(StringComparer.Ordinal))
        {
            string from = $"{mine}/{name}";
            if (!folder.Available)
            {
                throw new IOException("The sync folder went away while old change files were being tidied.");
            }

            if (folder.Read(from) is not { } bytes)
            {
                continue;
            }

            string to = $"{SyncEngine.Root}/trash/{me}/{stamp}{Mark}{name}";
            folder.Write(to, bytes);
            if (folder.Read(to) is not { } back || !back.AsSpan().SequenceEqual(bytes))
            {
                throw new IOException("A tidied change file did not read back whole from the trash, so it was left where it was.");
            }

            folder.Delete(from);
            done.Moved++;
            done.MovedBytes += bytes.Length;
        }
    }

    /// <summary>Takes away what this device moved to the trash at least its days ago, by the time it was moved, never the file's own.</summary>
    private static void Empty(ISyncFolder folder, string me, DateTime nowUtc, SyncOptions options, Counted done)
    {
        string trash = $"{SyncEngine.Root}/trash/{me}";
        foreach (string name in folder.Files(trash))
        {
            // A copy that was never renamed into place begins with a full stop and carries the same time.
            string bare = name.TrimStart('.');
            int mark = bare.IndexOf(Mark, StringComparison.Ordinal);
            if (mark <= 0 || !DateTime.TryParseExact(bare[..mark], "yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var moved) || nowUtc - moved < TimeSpan.FromDays(options.TrashDays))
            {
                continue;
            }

            long size = folder.Read($"{trash}/{name}")?.LongLength ?? 0;
            folder.Delete($"{trash}/{name}");
            done.Removed += name.StartsWith('.') ? 0 : 1;
            done.FreedBytes += size;
        }
    }

    /// <summary>Every device but this one that has written to the folder: a change folder, a device file or a read position.</summary>
    private static SortedSet<string> Devices(ISyncFolder folder, string me)
    {
        static string? Stem(string file) => file.EndsWith(".json", StringComparison.Ordinal) && !file.StartsWith('.') ? file[..^".json".Length] : null;
        var all = new SortedSet<string>(StringComparer.Ordinal);
        all.UnionWith(folder.Folders($"{SyncEngine.Root}/changes"));
        all.UnionWith(folder.Files($"{SyncEngine.Root}/devices").Select(Stem).OfType<string>());
        all.UnionWith(folder.Files($"{SyncEngine.Root}/read").Select(Stem).OfType<string>());
        all.Remove(me);
        return all;
    }

    /// <summary>A read position, or null where it is missing, not whole, not this format, or not the device's own.</summary>
    private static Position? ReadPosition(byte[]? bytes, string device)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            if (JsonNode.Parse(bytes) is not JsonObject file || (string?)file["format"] != PositionFormat || file["end"]?.GetValueKind() != JsonValueKind.True
                || (string?)file["device"] != device || (int?)file["version"] is not { } version || version < 1 || version > SyncEngine.Version
                || file["read"] is not JsonObject read || file["waiting"] is not JsonArray waiting)
            {
                return null;
            }

            var byDevice = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var (writer, names) in read)
            {
                if (names is not JsonArray list)
                {
                    return null;
                }

                byDevice[writer] = [.. list.Select(n => (string?)n).OfType<string>()];
            }

            var held = new List<(string, string)>();
            foreach (var node in waiting)
            {
                if (node is not JsonObject w || (string?)w["kind"] is not { } kind || (string?)w["key"] is not { } key)
                {
                    // A waiting record that cannot be read could be any record: not seen.
                    return null;
                }

                held.Add((kind, key));
            }

            return new Position(byDevice, held);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}
