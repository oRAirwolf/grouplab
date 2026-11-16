using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Records;

/// <summary>A pool's file as read: how its sheets are pooled, and each sheet's own session file read.</summary>
public sealed record PoolPackageContents(string Choices, IReadOnlyList<SessionPackageContents> Sheets, string Device);

/// <summary>What opening a pool's file did: the pool saved here, and how many of its sheets were new and how many were here already.</summary>
public sealed record PoolBroughtIn(PoolRecord Pool, int Added, int AlreadyHere);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 495 section 4: a pooled set shared as one file, so it opens whole on another GroupLab. The same ending as a
/// session file (<see cref="SessionPackage.Extension"/>) and a zip like one: <c>session.json</c> names the format, <c>grouplab-pool-1</c>,
/// which a GroupLab from before pools reads as a newer file and says so, and then each sheet's own session file, <c>sheet-1.grouplab</c> on,
/// as <see cref="SessionPackage"/> writes one. Nothing of a sheet travels that its own session file would not carry.
/// <para>
/// <b>Opening one is reading a stranger's file</b>, as a session file is: only those names are read, each within its size, and nothing in
/// it is acted on. Each sheet comes in as a session first unless the same picture with the same holes is already here.
/// </para>
/// </summary>
public static class PoolPackage
{
    public const string Schema = "grouplab-pool-1";

    /// <summary>More sheets than any set Made for your optic prints, and more pictures than anyone pools by hand.</summary>
    public const int MostSheets = 60;

    private const long MostSheetBytes = 48L * 1024 * 1024;

    private const long MostJsonBytes = 64 * 1024;

    /// <summary>Whether a file with a session file's ending is a pool's file, from its first entry; false for anything else.</summary>
    public static bool IsPool(Stream from)
    {
        ArgumentNullException.ThrowIfNull(from);
        long at = from.CanSeek ? from.Position : 0;
        try
        {
            using var zip = new ZipArchive(from, ZipArchiveMode.Read, leaveOpen: true);
            return Json(zip, "session.json") is { } session && (string?)session["schema"] == Schema;
        }
        catch (Exception e) when (e is InvalidDataException or JsonException or IOException or NotSupportedException or InvalidOperationException)
        {
            return false;
        }
        finally
        {
            if (from.CanSeek)
            {
                from.Position = at;
            }
        }
    }

    /// <summary>Writes the pool's file from each sheet's session file, already written by <see cref="SessionPackage.Write"/>.</summary>
    public static void Write(Stream to, string choices, IReadOnlyList<byte[]> sheets, string device, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(sheets);
        if (sheets.Count is 0 or > MostSheets)
        {
            throw new ArgumentException($"A pool's file holds 1 to {MostSheets} sheets.", nameof(sheets));
        }

        using var zip = new ZipArchive(to, ZipArchiveMode.Create, leaveOpen: true);
        using (var writer = new StreamWriter(zip.CreateEntry("session.json", CompressionLevel.Optimal).Open(), new UTF8Encoding(false)))
        {
            writer.Write(new JsonObject
            {
                ["schema"] = Schema,
                ["sheets"] = sheets.Count,
                ["choices"] = choices,
                ["device"] = device.Length > 80 ? device[..80] : device,
                ["written"] = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            }.ToJsonString());
        }

        for (int i = 0; i < sheets.Count; i++)
        {
            using var stream = zip.CreateEntry(SheetName(i), CompressionLevel.NoCompression).Open();
            stream.Write(sheets[i]);
        }
    }

    /// <summary>Reads a pool's file, refusing anything that is not one with the reason.</summary>
    public static PoolPackageContents Read(Stream from)
    {
        ArgumentNullException.ThrowIfNull(from);
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(from, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw new SessionPackageException("This is not a GroupLab pooled set file.");
        }

        try
        {
            var session = Json(zip, "session.json") ?? throw new SessionPackageException("This is not a GroupLab pooled set file: it says nothing of what it holds.");
            if ((string?)session["schema"] != Schema)
            {
                throw new SessionPackageException("This is not a GroupLab pooled set file.");
            }

            int count = session["sheets"] is JsonValue c && c.TryGetValue(out int n) ? n : 0;
            if (count is < 1 or > MostSheets)
            {
                throw new SessionPackageException("This pooled set file is damaged, so it was not opened. Share it again from the device it came from.");
            }

            var sheets = new List<SessionPackageContents>();
            for (int i = 0; i < count; i++)
            {
                var entry = zip.GetEntry(SheetName(i)) ?? throw new SessionPackageException(
                    string.Create(CultureInfo.InvariantCulture, $"This pooled set file is missing sheet {i + 1} of {count}, so it was not opened. Share it again from the device it came from."));
                byte[] bytes = Bytes(entry, MostSheetBytes) ?? throw new SessionPackageException("A sheet in this pooled set file is larger than GroupLab opens.");
                using var inner = new MemoryStream(bytes);
                sheets.Add(SessionPackage.Read(inner));
            }

            string device = (string?)session["device"] ?? "";
            return new PoolPackageContents((string?)session["choices"] ?? SessionPools.Choices(pictures: false), sheets, device.Length > 80 ? device[..80] : device);
        }
        catch (Exception e) when (e is InvalidDataException or InvalidOperationException or JsonException or FormatException or ArgumentException or IOException
            or NotSupportedException or OverflowException)
        {
            throw new SessionPackageException("This pooled set file is damaged, so it was not opened. Share it again from the device it came from.");
        }
        finally
        {
            zip.Dispose();
        }
    }

    /// <summary>
    /// Brings a pool's file in: each sheet unpacked into its own folder under <paramref name="folder"/> and saved as a session, unless the same
    /// picture with the same holes is already here, then the pool saved naming them. <paramref name="build"/> is the platform's own record
    /// for a marking, with its proof image, as Accept and analyze makes it.
    /// </summary>
    public static PoolBroughtIn BringIn(SessionStore store, PoolPackageContents contents, string folder, Func<MarkingState, TargetDefinition?, string, SessionRecord> build,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(build);
        var ids = new List<long>();
        int added = 0, already = 0;
        for (int i = 0; i < contents.Sheets.Count; i++)
        {
            var sheet = contents.Sheets[i];
            var state = SessionPackage.Unpack(sheet, Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"sheet-{i + 1}")));
            string hash = Convert.ToHexString(SHA256.HashData(sheet.Image)).ToLowerInvariant();
            var record = build(state, sheet.Definition, hash);
            long? here = null;
            foreach (long id in store.WithImage(hash, record.DefinitionId))
            {
                if (store.Get(id) is { } before && SessionRecords.SameHoles(state, MarkingFile.Read(before.MarkingJson).State, Tolerance(state)))
                {
                    here = id;
                    break;
                }
            }

            if (here is { } found)
            {
                ids.Add(found);
                already++;
            }
            else
            {
                ids.Add(store.Save(record with { Id = 0 }));
                added++;
            }
        }

        var pool = SessionPools.Save(store, null, ids, contents.Choices, utcNow);
        return new PoolBroughtIn(pool, added, already);
    }

    /// <summary>What a pool's file opened here says.</summary>
    public static string OpenedWords(PoolBroughtIn brought, string device)
    {
        ArgumentNullException.ThrowIfNull(brought);
        int n = brought.Pool.Sheets.Count;
        string from = device.Length > 0 ? device : "another device";
        string sheets = brought.AlreadyHere == 0
            ? string.Create(CultureInfo.InvariantCulture, $"its {n} sheet{(n == 1 ? " is" : "s are")} saved in Session records as sessions of their own")
            : string.Create(CultureInfo.InvariantCulture, $"{brought.Added} of its sheets {(brought.Added == 1 ? "is" : "are")} new in Session records and {brought.AlreadyHere} {(brought.AlreadyHere == 1 ? "was" : "were")} here already");
        return $"Opened a pooled set from {from}: {sheets}, and the pool is saved with them.";
    }

    /// <summary>A quarter of an inch where the scale is known; on a picture with no scale, 25 pixels, as the computer's save takes it.</summary>
    private static double Tolerance(MarkingState state) =>
        state.Scale is { } scale && state.Shots.FirstOrDefault() is { } first ? 0.25 * HoleSize.PixelsPerInch(scale, first.Image) : 25;

    private static string SheetName(int i) => string.Create(CultureInfo.InvariantCulture, $"sheet-{i + 1}{SessionPackage.Extension}");

    private static JsonObject? Json(ZipArchive zip, string name)
    {
        if (zip.GetEntry(name) is not { } entry || Bytes(entry, MostJsonBytes) is not { } bytes)
        {
            return null;
        }

        return JsonNode.Parse(Encoding.UTF8.GetString(bytes)) as JsonObject;
    }

    private static byte[]? Bytes(ZipArchiveEntry entry, long most)
    {
        if (entry.Length > most)
        {
            return null;
        }

        using var stream = entry.Open();
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (copy.Length + read > most)
            {
                return null;
            }

            copy.Write(buffer, 0, read);
        }

        return copy.ToArray();
    }
}
